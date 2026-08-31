import { Component, DestroyRef, ElementRef, ViewChild, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BaseEvent, EventType } from '@ag-ui/core';
import { forkJoin, Subscription } from 'rxjs';
import { AgUiCopilotService } from '../../services/ag-ui-copilot.service';
import { CopilotActivity, CopilotActivityStatus, CopilotApprovalRequest, CopilotConversation, CopilotMessage, CopilotTurn } from '../../models/copilot.model';
import { MarkdownPipe } from '../../pipes/markdown.pipe';
import { ActivityTimeline } from '../../components/activity-timeline/activity-timeline';
import { ApprovalCard } from '../../components/approval-card/approval-card';
import { ConversationSidebar } from '../../components/conversation-sidebar/conversation-sidebar';
import { InterruptedTurnActions } from '../../components/interrupted-turn-actions/interrupted-turn-actions';
import { PromptEditor } from '../../components/prompt-editor/prompt-editor';
import { CompletedTurnActions } from '../../components/completed-turn-actions/completed-turn-actions';

@Component({
  selector: 'app-copilot-chat',
  standalone: true,
  imports: [MarkdownPipe, ActivityTimeline, ApprovalCard, ConversationSidebar, InterruptedTurnActions, PromptEditor, CompletedTurnActions],
  templateUrl: './copilot-chat.html',
  styleUrl: './copilot-chat.scss'
})
export class CopilotChat {
  private readonly copilotService = inject(AgUiCopilotService);
  private readonly destroyRef = inject(DestroyRef);

  @ViewChild('messagesContainer')
  private messagesContainer?: ElementRef<HTMLDivElement>;

  private shouldAutoScroll = true;
  private scrollFrameId: number | null = null;
  private readonly autoScrollThreshold = 80;
  private activeRunSubscription: Subscription | null = null;
  private activeAssistantMessageId: string | null = null;
  private activeUserMessageId: string | null = null;

  readonly message = signal('');
  readonly messages = signal<CopilotMessage[]>([]);
  readonly isLoadingHistory = signal(false);
  readonly isSending = signal(false);
  readonly runStopped = signal(false);
  readonly errorMessage = signal('');
  readonly pendingApproval = signal<CopilotApprovalRequest | null>(null);

  // Conversation history
  readonly conversations = signal<CopilotConversation[]>([]);
  readonly conversationPageNumber = signal(1);
  readonly conversationTotalCount = signal(0);
  readonly conversationTotalPages = signal(0);
  readonly conversationHasPreviousPage = signal(false);
  readonly conversationHasNextPage = signal(false);
  readonly conversationPageSize = 10;
  readonly isLoadingConversations = signal(false);
  readonly currentThreadId = signal('');

  // Conversation menu / rename
  readonly openConversationMenuThreadId = signal<string | null>(null);
  readonly renamingConversationThreadId = signal<string | null>(null);
  readonly renameConversationTitle = signal('');
  readonly managingConversationThreadId = signal<string | null>(null);

  readonly editingUserMessageId = signal<string | null>(null);
  readonly editedPrompt = signal('');
  readonly editingCompletedTurn = signal(false);

  // Delete confirmation
  readonly conversationPendingDelete = signal<CopilotConversation | null>(null);

  // Tool activity
  readonly activities = signal<CopilotActivity[]>([]);
  readonly activityExpanded = signal(false);

  private readonly toolCalls = new Map<string, {
    name: string;
    arguments: string;
  }>();

  constructor() {
    this.destroyRef.onDestroy(() => {
      if (this.scrollFrameId !== null) {
        cancelAnimationFrame(this.scrollFrameId);
      }

      this.activeRunSubscription?.unsubscribe();
      this.activeRunSubscription = null;
    });

    this.copilotService.ensureSession();
    this.currentThreadId.set(this.copilotService.threadId);

    this.copilotService.conversationSaved$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.loadConversations(1);
      });

    this.loadConversations();
    this.loadConversationState();
  }

  private refreshPendingApproval(fallback: CopilotApprovalRequest): void {
    this.pendingApproval.set(fallback);

    this.copilotService.getPendingApproval().subscribe({
      next: approval => {
        if (approval) {
          this.pendingApproval.set(approval);
        }
      },
      error: error => {
        console.error('Failed to load approval presentation:', error);
      }
    });
  }

  private startActivity(toolCallId: string, toolName: string): void {
    const existing = this.activities().some(activity => activity.id === toolCallId);

    if (existing) {
      this.setActivityStatus(toolCallId, 'running');
      this.scheduleScrollToBottom();
      return;
    }

    this.activities.update(activities => [
      ...activities,
      {
        id: toolCallId,
        toolName,
        status: 'running'
      }
    ]);

    this.scheduleScrollToBottom();
  }

  private setActivityStatus(toolCallId: string, status: CopilotActivityStatus): void {
    this.activities.update(activities =>
      activities.map(activity =>
        activity.id === toolCallId
          ? {
            ...activity,
            status
          }
          : activity
      )
    );
  }

  private failRunningActivities(): void {
    this.activities.update(activities =>
      activities.map(activity =>
        activity.status === 'running'
          ? {
            ...activity,
            status: 'failed'
          }
          : activity
      )
    );
  }

  private stopRunningActivities(): void {
    this.activities.update(activities =>
      activities.map(activity =>
        activity.status === 'running'
          ? {
            ...activity,
            status: 'stopped'
          }
          : activity
      )
    );
  }

  private persistStoppedTurn(userMessageId: string, activities: CopilotActivity[]): void {
    this.copilotService.stopTurn(userMessageId, activities).subscribe({
      error: error => {
        console.error('Failed to persist stopped Copilot turn:', error);
      }
    });
  }

  private getMessageActivities(messageId: string): CopilotActivity[] {
    return this.messages().find(message => message.id === messageId)?.activities ?? [];
  }

  toggleMessageActivity(messageId: string): void {
    this.messages.update(messages =>
      messages.map(message =>
        message.id === messageId
          ? {
            ...message,
            activityExpanded: !(message.activityExpanded ?? true)
          }
          : message
      )
    );
  }

  private loadConversationState(): void {
    const threadId = this.copilotService.threadId;

    this.isLoadingHistory.set(true);
    this.errorMessage.set('');

    forkJoin({
      history: this.copilotService.getHistory(),
      turns: this.copilotService.getTurns(),
      pendingApproval: this.copilotService.getPendingApproval()
    }).subscribe({
      next: result => {
        if (this.currentThreadId() !== threadId) {
          return;
        }

        const stoppedTurns = new Map<string, CopilotTurn>(
          result.turns
            .filter(turn => turn.status === 'Stopped')
            .map(turn => [turn.userMessageId, turn])
        );

        const messages: CopilotMessage[] = [];

        for (const historyMessage of result.history) {
          messages.push({
            id: historyMessage.id,
            role: historyMessage.role,
            content: historyMessage.content,
            createdAt: historyMessage.createdAt ? new Date(historyMessage.createdAt) : null
          });

          if (historyMessage.role !== 'user') {
            continue;
          }

          const stoppedTurn = stoppedTurns.get(historyMessage.id);

          if (!stoppedTurn) {
            continue;
          }

          messages.push({
            id: `stopped-${historyMessage.id}`,
            role: 'assistant',
            content: '',
            createdAt: new Date(stoppedTurn.updatedAt),
            activities: stoppedTurn.activities,
            turnStopped: true,
            activityExpanded: true,
            turnUserMessageId: stoppedTurn.userMessageId
          });
        }

        this.messages.set(messages);
        this.shouldAutoScroll = true;
        this.scheduleScrollToBottom(true);
        this.pendingApproval.set(result.pendingApproval);
      },
      error: error => {
        console.error('Failed to load Copilot conversation:', error);

        if (this.currentThreadId() === threadId) {
          this.errorMessage.set('The conversation could not be loaded.');
          this.isLoadingHistory.set(false);
        }
      },
      complete: () => {
        if (this.currentThreadId() === threadId) {
          this.isLoadingHistory.set(false);
        }
      }
    });
  }

  private loadConversations(pageNumber: number = this.conversationPageNumber()): void {
    this.isLoadingConversations.set(true);

    this.copilotService.getConversations(pageNumber, this.conversationPageSize).subscribe({
      next: result => {
        this.conversations.set(result.items);
        this.conversationPageNumber.set(result.pageNumber);
        this.conversationTotalCount.set(result.totalCount);
        this.conversationTotalPages.set(result.totalPages);
        this.conversationHasPreviousPage.set(result.hasPreviousPage);
        this.conversationHasNextPage.set(result.hasNextPage);
      },
      error: error => {
        console.error('Failed to load Copilot conversations:', error);
        this.isLoadingConversations.set(false);
      },
      complete: () => {
        this.isLoadingConversations.set(false);
      }
    });
  }

  stopGeneration(): void {
    if (!this.isSending()) {
      return;
    }

    const assistantMessageId = this.activeAssistantMessageId;
    const userMessageId = this.activeUserMessageId;
    const agentRunStarted = this.activeRunSubscription !== null;

    this.activeRunSubscription?.unsubscribe();
    this.activeRunSubscription = null;

    this.runStopped.set(true);
    this.stopRunningActivities();

    const stoppedActivities: CopilotActivity[] = this.activities().map(activity => ({
      ...activity,
      status: activity.status === 'running' ? 'stopped' : activity.status
    }));

    if (assistantMessageId) {
      this.messages.update(messages =>
        messages.map(message =>
          message.id === assistantMessageId
            ? {
              ...message,
              activities: stoppedActivities,
              turnStopped: true,
              activityExpanded: true,
              turnUserMessageId: userMessageId ?? undefined
            }
            : message
        )
      );
    }

    /*
     * If AG-UI has already started, prepare-turn has
     * completed and the CopilotTurn row already exists.
     */
    if (userMessageId && agentRunStarted) {
      this.persistStoppedTurn(userMessageId, stoppedActivities);
    }

    this.copilotService.stopCurrentRun();

    this.activeAssistantMessageId = null;
    this.activeUserMessageId = null;
    this.isSending.set(false);
    this.errorMessage.set('');
    this.activityExpanded.set(true);
    this.shouldAutoScroll = true;
    this.scheduleScrollToBottom(true);
  }

  toggleActivity(): void {
    this.activityExpanded.update(expanded => !expanded);
    this.scheduleScrollToBottom();
  }

  startNewConversation(): void {
    if (this.isSending() || this.isLoadingHistory()) {
      return;
    }

    const threadId = this.copilotService.startNewConversation();

    this.activeRunSubscription = null;
    this.activeAssistantMessageId = null;
    this.activeUserMessageId = null;
    this.currentThreadId.set(threadId);
    this.messages.set([]);
    this.shouldAutoScroll = true;
    this.pendingApproval.set(null);
    this.activities.set([]);
    this.activityExpanded.set(false);
    this.runStopped.set(false);
    this.toolCalls.clear();
    this.message.set('');
    this.errorMessage.set('');
    this.closeConversationMenu();
    this.cancelRenameConversation();
    this.conversationPendingDelete.set(null);
    this.isLoadingHistory.set(false);
    this.editingUserMessageId.set(null);
    this.editedPrompt.set('');
  }

  openConversation(conversation: CopilotConversation): void {
    if (this.isSending() || this.isLoadingHistory()) {
      return;
    }

    if (conversation.threadId === this.currentThreadId()) {
      return;
    }

    this.closeConversationMenu();
    this.cancelRenameConversation();
    this.conversationPendingDelete.set(null);
    this.activeRunSubscription = null;
    this.activeAssistantMessageId = null;
    this.activeUserMessageId = null;

    this.copilotService.openConversation(conversation);

    this.currentThreadId.set(conversation.threadId);
    this.messages.set([]);
    this.pendingApproval.set(null);
    this.activities.set([]);
    this.activityExpanded.set(false);
    this.runStopped.set(false);
    this.toolCalls.clear();
    this.message.set('');
    this.errorMessage.set('');
    this.shouldAutoScroll = true;
    this.editingUserMessageId.set(null);
    this.editedPrompt.set('');

    this.loadConversationState();
  }

  onMessagesScroll(event: Event): void {
    const container = event.target as HTMLDivElement;

    const distanceFromBottom =
      container.scrollHeight -
      container.scrollTop -
      container.clientHeight;

    this.shouldAutoScroll = distanceFromBottom <= this.autoScrollThreshold;
  }

  onMessageInput(event: Event): void {
    const input = event.target as HTMLTextAreaElement;
    this.message.set(input.value);
  }

  private scheduleScrollToBottom(force = false): void {
    if (!force && !this.shouldAutoScroll) {
      return;
    }

    if (this.scrollFrameId !== null) {
      cancelAnimationFrame(this.scrollFrameId);
    }

    this.scrollFrameId = requestAnimationFrame(() => {
      this.scrollFrameId = null;

      const container = this.messagesContainer?.nativeElement;

      if (!container) {
        return;
      }

      container.scrollTop = container.scrollHeight;
    });
  }

  private isInterruptOutcome(outcome: unknown): outcome is {
    type: 'interrupt';
    interrupts: Array<{
      id: string;
      reason: string;
      message?: string;
      toolCallId?: string;
    }>;
  } {
    if (typeof outcome !== 'object' || outcome === null) {
      return false;
    }

    const value = outcome as {
      type?: unknown;
      interrupts?: unknown;
    };

    return value.type === 'interrupt' && Array.isArray(value.interrupts);
  }

  private handleAgUiEvent(event: BaseEvent, assistantMessageId: string): void {
    /*
     * Streaming assistant response
     */
    if (event.type === EventType.TEXT_MESSAGE_CONTENT) {
      const delta = event['delta'];

      if (typeof delta === 'string') {
        this.messages.update(messages =>
          messages.map(message =>
            message.id === assistantMessageId
              ? {
                ...message,
                content: message.content + delta
              }
              : message
          )
        );

        this.scheduleScrollToBottom();
      }
    }

    /*
     * Tool execution starts
     */
    if (event.type === EventType.TOOL_CALL_START) {
      const toolCallId = event['toolCallId'];
      const toolCallName = event['toolCallName'];

      if (typeof toolCallId === 'string' && typeof toolCallName === 'string') {
        this.toolCalls.set(toolCallId, {
          name: toolCallName,
          arguments: ''
        });

        this.startActivity(toolCallId, toolCallName);
      }

      /*
       * Remove temporary narration such as
       * "I'll look that up..." once a real tool call begins.
       */
      this.messages.update(messages =>
        messages.map(message =>
          message.id === assistantMessageId
            ? {
              ...message,
              content: ''
            }
            : message
        )
      );
    }

    /*
     * Tool arguments stream
     */
    if (event.type === EventType.TOOL_CALL_ARGS) {
      const toolCallId = event['toolCallId'];
      const delta = event['delta'];

      if (typeof toolCallId === 'string' && typeof delta === 'string') {
        const toolCall = this.toolCalls.get(toolCallId);

        if (toolCall) {
          toolCall.arguments += delta;
        }
      }
    }

    /*
     * Tool execution completed
     */
    if (event.type === EventType.TOOL_CALL_RESULT) {
      const toolCallId = event['toolCallId'];

      if (typeof toolCallId === 'string') {
        this.setActivityStatus(toolCallId, 'completed');
      }
    }

    /*
     * Run completed or interrupted
     */
    if (event.type === EventType.RUN_FINISHED) {
      const outcome = event['outcome'];

      if (
        typeof outcome === 'object' &&
        outcome !== null &&
        'type' in outcome &&
        outcome.type === 'success'
      ) {
        const userMessageId = this.activeUserMessageId;

        if (userMessageId) {
          const assistantMessage = this.messages().find(
            message => message.id === assistantMessageId
          );

          this.copilotService.completeTurn(
            userMessageId,
            assistantMessageId,
            assistantMessage?.content ?? '',
            this.activities()
          ).subscribe({
            error: error => {
              console.error('Failed to persist completed Copilot turn:', error);
            }
          });
        }

        this.pendingApproval.set(null);
        this.activityExpanded.set(false);
        return;
      }

      if (this.isInterruptOutcome(outcome)) {
        const interrupt = outcome.interrupts[0];

        if (!interrupt?.toolCallId) {
          return;
        }

        const toolCall = this.toolCalls.get(interrupt.toolCallId);

        if (!toolCall) {
          return;
        }

        this.setActivityStatus(interrupt.toolCallId, 'waiting');
        this.activityExpanded.set(true);

        const fallbackApproval: CopilotApprovalRequest = {
          interruptId: interrupt.id,
          toolCallId: interrupt.toolCallId,
          toolName: toolCall.name,
          arguments: toolCall.arguments,
          message: interrupt.message,
          displayTitle: this.humanizeToolName(toolCall.name),
          displayDetails: [],
          warning: null
        };

        this.refreshPendingApproval(fallbackApproval);
        this.scheduleScrollToBottom();
        return;
      }

      this.activityExpanded.set(false);
    }
  }

  private humanizeToolName(toolName: string): string {
    const value = toolName
      .replace(/_/g, ' ')
      .replace(/([a-z])([A-Z])/g, '$1 $2')
      .toLowerCase();

    return value.charAt(0).toUpperCase() + value.slice(1);
  }

  sendMessage(): void {
    const text = this.message().trim();

    if (!text || this.isSending() || this.isLoadingHistory()) {
      return;
    }

    this.activities.set([]);
    this.activityExpanded.set(true);
    this.runStopped.set(false);

    const userMessage: CopilotMessage = {
      id: crypto.randomUUID(),
      role: 'user',
      content: text,
      createdAt: new Date()
    };

    const assistantMessage: CopilotMessage = {
      id: crypto.randomUUID(),
      role: 'assistant',
      content: '',
      createdAt: new Date()
    };

    this.shouldAutoScroll = true;

    this.messages.update(messages => [
      ...messages,
      userMessage,
      assistantMessage
    ]);

    this.scheduleScrollToBottom(true);
    this.pendingApproval.set(null);
    this.toolCalls.clear();
    this.message.set('');
    this.errorMessage.set('');
    this.closeConversationMenu();
    this.isSending.set(true);
    this.activeAssistantMessageId = assistantMessage.id;
    this.activeUserMessageId = userMessage.id;

    /*
     * Persist the user's message and create the
     * CopilotTurn before starting AG-UI.
     */
    this.copilotService.prepareTurn(userMessage.id, text)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          const activeTurnChanged = this.activeAssistantMessageId !== assistantMessage.id;

          /*
           * Stop may have happened while prepare-turn
           * was still being persisted.
           */
          if (activeTurnChanged) {
            const stoppedMessage = this.messages().find(message => message.id === assistantMessage.id);

            if (stoppedMessage?.turnStopped) {
              this.persistStoppedTurn(userMessage.id, stoppedMessage.activities ?? []);
            }

            return;
          }

          if (this.runStopped()) {
            this.persistStoppedTurn(
              userMessage.id,
              this.getMessageActivities(assistantMessage.id)
            );
            return;
          }

          this.activeRunSubscription = this.copilotService.runPreparedTurn().subscribe({
            next: event => {
              this.handleAgUiEvent(event, assistantMessage.id);
            },
            error: error => {
              console.error('AG-UI error:', error);

              this.failRunningActivities();
              this.removeEmptyAssistantMessage(assistantMessage.id);
              this.errorMessage.set('Something went wrong while contacting the assistant.');
              this.activeRunSubscription = null;
              this.activeAssistantMessageId = null;
              this.activeUserMessageId = null;
              this.isSending.set(false);
            },
            complete: () => {
              this.removeEmptyAssistantMessage(assistantMessage.id);
              this.activeRunSubscription = null;
              this.activeAssistantMessageId = null;
              this.activeUserMessageId = null;
              this.isSending.set(false);
            }
          });
        },
        error: error => {
          console.error('Failed to prepare Copilot turn:', error);

          this.removeEmptyAssistantMessage(assistantMessage.id);

          if (this.activeAssistantMessageId === assistantMessage.id) {
            this.errorMessage.set('The message could not be saved before starting the assistant.');
            this.activeAssistantMessageId = null;
            this.activeUserMessageId = null;
            this.isSending.set(false);
          }
        }
      });
  }

  respondToApproval(approved: boolean): void {
    const approval = this.pendingApproval();

    if (!approval || this.isSending()) {
      return;
    }

    this.activityExpanded.set(true);
    this.pendingApproval.set(null);
    this.runStopped.set(false);

    this.setActivityStatus(
      approval.toolCallId,
      approved ? 'running' : 'rejected'
    );

    this.toolCalls.clear();

    const assistantMessage: CopilotMessage = {
      id: crypto.randomUUID(),
      role: 'assistant',
      content: '',
      createdAt: new Date()
    };

    this.shouldAutoScroll = true;

    this.messages.update(messages => [
      ...messages,
      assistantMessage
    ]);

    this.scheduleScrollToBottom(true);
    this.errorMessage.set('');
    this.isSending.set(true);
    this.activeAssistantMessageId = assistantMessage.id;
    this.activeUserMessageId = [...this.messages()]
      .reverse()
      .find(message => message.role === 'user')
      ?.id ?? null;

    this.activeRunSubscription = this.copilotService.resumeApproval(approval, approved).subscribe({
      next: event => {
        this.handleAgUiEvent(event, assistantMessage.id);
      },
      error: error => {
        console.error('AG-UI approval resume error:', error);

        this.setActivityStatus(approval.toolCallId, 'waiting');
        this.removeEmptyAssistantMessage(assistantMessage.id);
        this.pendingApproval.set(approval);
        this.errorMessage.set('Something went wrong while processing the approval.');
        this.activeRunSubscription = null;
        this.activeAssistantMessageId = null;
        this.activeUserMessageId = null;
        this.isSending.set(false);
      },
      complete: () => {
        this.removeEmptyAssistantMessage(assistantMessage.id);
        this.activeRunSubscription = null;
        this.activeAssistantMessageId = null;
        this.activeUserMessageId = null;
        this.isSending.set(false);
      }
    });
  }

  toggleConversationMenu(threadId: string, event: Event): void {
    event.stopPropagation();

    this.openConversationMenuThreadId.update(current =>
      current === threadId ? null : threadId
    );
  }

  closeConversationMenu(): void {
    this.openConversationMenuThreadId.set(null);
  }

  beginRenameConversation(conversation: CopilotConversation, event: Event): void {
    event.stopPropagation();
    this.renamingConversationThreadId.set(conversation.threadId);
    this.renameConversationTitle.set(conversation.title);
    this.closeConversationMenu();
  }

  onRenameConversationInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.renameConversationTitle.set(input.value);
  }

  cancelRenameConversation(event?: Event): void {
    event?.stopPropagation();
    this.renamingConversationThreadId.set(null);
    this.renameConversationTitle.set('');
  }

  saveRenameConversation(conversation: CopilotConversation, event: Event): void {
    event.stopPropagation();

    const title = this.renameConversationTitle().trim();

    if (!title) {
      return;
    }

    if (title === conversation.title) {
      this.cancelRenameConversation();
      return;
    }

    this.managingConversationThreadId.set(conversation.threadId);
    this.errorMessage.set('');

    this.copilotService.renameConversation(conversation.threadId, title).subscribe({
      next: updatedConversation => {
        this.conversations.update(conversations =>
          conversations.map(item =>
            item.threadId === updatedConversation.threadId
              ? updatedConversation
              : item
          )
        );

        this.cancelRenameConversation();
      },
      error: error => {
        console.error('Failed to rename Copilot conversation:', error);
        this.errorMessage.set('The conversation could not be renamed.');
        this.managingConversationThreadId.set(null);
      },
      complete: () => {
        this.managingConversationThreadId.set(null);
      }
    });
  }

  requestDeleteConversation(conversation: CopilotConversation, event: Event): void {
    event.stopPropagation();
    this.closeConversationMenu();
    this.conversationPendingDelete.set(conversation);
  }

  cancelDeleteConversation(): void {
    const conversation = this.conversationPendingDelete();

    if (conversation && this.managingConversationThreadId() === conversation.threadId) {
      return;
    }

    this.conversationPendingDelete.set(null);
  }

  deleteConversation(): void {
    const conversation = this.conversationPendingDelete();

    if (!conversation) {
      return;
    }

    if (this.managingConversationThreadId() === conversation.threadId) {
      return;
    }

    this.managingConversationThreadId.set(conversation.threadId);
    this.errorMessage.set('');

    this.copilotService.deleteConversation(conversation.threadId).subscribe({
      next: () => {
        if (conversation.threadId === this.currentThreadId()) {
          const threadId = this.copilotService.startNewConversation();

          this.activeRunSubscription = null;
          this.activeAssistantMessageId = null;
          this.activeUserMessageId = null;
          this.currentThreadId.set(threadId);
          this.messages.set([]);
          this.pendingApproval.set(null);
          this.activities.set([]);
          this.activityExpanded.set(false);
          this.runStopped.set(false);
          this.toolCalls.clear();
          this.message.set('');
          this.errorMessage.set('');
          this.shouldAutoScroll = true;
        }

        const remainingCount = Math.max(
          0,
          this.conversationTotalCount() - 1
        );

        const remainingPages =
          remainingCount === 0
            ? 0
            : Math.ceil(
              remainingCount /
              this.conversationPageSize
            );

        const pageToLoad = Math.min(
          this.conversationPageNumber(),
          Math.max(1, remainingPages)
        );

        this.conversationPendingDelete.set(null);
        this.loadConversations(pageToLoad);
      },
      error: error => {
        console.error('Failed to delete Copilot conversation:', error);
        this.errorMessage.set('The conversation could not be deleted.');
        this.managingConversationThreadId.set(null);
      },
      complete: () => {
        this.managingConversationThreadId.set(null);
      }
    });
  }

  previousConversationPage(): void {
    if (this.isLoadingConversations() || !this.conversationHasPreviousPage()) {
      return;
    }

    this.closeConversationMenu();
    this.cancelRenameConversation();
    this.loadConversations(this.conversationPageNumber() - 1);
  }

  nextConversationPage(): void {
    if (this.isLoadingConversations() || !this.conversationHasNextPage()) {
      return;
    }

    this.closeConversationMenu();
    this.cancelRenameConversation();
    this.loadConversations(this.conversationPageNumber() + 1);
  }

  canRetryStoppedTurn(userMessageId?: string): boolean {
    if (!userMessageId) {
      return false;
    }

    const latestUserMessage = [...this.messages()]
      .reverse()
      .find(message => message.role === 'user');

    return latestUserMessage?.id === userMessageId;
  }

  retryStoppedTurn(message: CopilotMessage): void {
    const userMessageId = message.turnUserMessageId;

    if (
      !userMessageId ||
      !message.turnStopped ||
      this.isSending() ||
      this.isLoadingHistory()
    ) {
      return;
    }

    if (!this.canRetryStoppedTurn(userMessageId)) {
      this.errorMessage.set('Only the latest interrupted request can be retried.');
      return;
    }

    const originalUserMessage = this.messages().find(
      item =>
        item.id === userMessageId &&
        item.role === 'user'
    );

    if (!originalUserMessage) {
      this.errorMessage.set('The original message for this retry could not be found.');
      return;
    }

    this.errorMessage.set('');
    this.pendingApproval.set(null);
    this.activities.set([]);
    this.activityExpanded.set(true);
    this.runStopped.set(false);
    this.toolCalls.clear();
    this.isSending.set(true);

    this.activeUserMessageId = userMessageId;
    this.activeAssistantMessageId = message.id;

    this.copilotService.retryTurn(userMessageId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          /*
           * The user could press Stop while the retry
           * preparation request was still executing.
           */
          if (this.runStopped()) {
            this.persistStoppedTurn(
              userMessageId,
              this.getMessageActivities(message.id)
            );
            return;
          }

          /*
           * Convert the persisted stopped assistant
           * placeholder back into a live assistant turn.
           */
          this.messages.update(messages =>
            messages.map(item =>
              item.id === message.id
                ? {
                  ...item,
                  content: '',
                  activities: [],
                  turnStopped: false,
                  activityExpanded: true
                }
                : item
            )
          );

          this.shouldAutoScroll = true;
          this.scheduleScrollToBottom(true);

          /*
           * The backend retained the original user
           * message and removed only the stopped marker.
           * Therefore run with messages: [].
           */
          this.activeRunSubscription = this.copilotService.runPreparedTurn().subscribe({
            next: event => {
              this.handleAgUiEvent(event, message.id);
            },
            error: error => {
              console.error('AG-UI retry error:', error);

              this.failRunningActivities();
              this.errorMessage.set('Something went wrong while retrying the response.');
              this.activeRunSubscription = null;
              this.activeAssistantMessageId = null;
              this.activeUserMessageId = null;
              this.isSending.set(false);
            },
            complete: () => {
              this.removeEmptyAssistantMessage(message.id);
              this.activeRunSubscription = null;
              this.activeAssistantMessageId = null;
              this.activeUserMessageId = null;
              this.isSending.set(false);
            }
          });
        },
        error: error => {
          console.error('Failed to prepare Copilot retry:', error);

          this.errorMessage.set(
            error?.status === 409
              ? 'Only the latest interrupted request can be retried.'
              : 'The interrupted response could not be retried.'
          );

          this.activeAssistantMessageId = null;
          this.activeUserMessageId = null;
          this.isSending.set(false);
        }
      });
  }

  beginEditStoppedPrompt(message: CopilotMessage): void {
    const userMessageId = message.turnUserMessageId;

    if (
      !userMessageId ||
      !message.turnStopped ||
      this.isSending() ||
      this.isLoadingHistory()
    ) {
      return;
    }

    if (!this.canRetryStoppedTurn(userMessageId)) {
      this.errorMessage.set('Only the latest interrupted request can be edited.');
      return;
    }

    const userMessage = this.messages().find(
      item =>
        item.id === userMessageId &&
        item.role === 'user'
    );

    if (!userMessage) {
      this.errorMessage.set('The original message could not be found.');
      return;
    }

    this.errorMessage.set('');
    this.editingUserMessageId.set(userMessageId);
    this.editedPrompt.set(userMessage.content);
    this.editingCompletedTurn.set(false);
  }

  onEditedPromptChange(value: string): void {
    this.editedPrompt.set(value);
  }

  cancelEditPrompt(): void {
    if (this.isSending()) {
      return;
    }

    this.editingCompletedTurn.set(false);
    this.editingUserMessageId.set(null);
    this.editedPrompt.set('');
  }

  canEditCompletedUserMessage(userMessageId: string): boolean {
    if (this.isSending() || this.isLoadingHistory()) {
      return false;
    }

    const messages = this.messages();

    const userIndex = messages.findIndex(
      message =>
        message.id === userMessageId &&
        message.role === 'user'
    );

    if (userIndex < 0) {
      return false;
    }

    const nextUserIndex = messages.findIndex(
      (message, index) =>
        index > userIndex &&
        message.role === 'user'
    );

    const endIndex =
      nextUserIndex >= 0
        ? nextUserIndex
        : messages.length;

    const assistantMessage = messages
      .slice(userIndex + 1, endIndex)
      .find(message =>
        message.role === 'assistant' &&
        !message.turnStopped &&
        !!message.content
      );

    if (!assistantMessage) {
      return false;
    }

    const latestCompletedAssistant = [...messages]
      .reverse()
      .find(message =>
        message.role === 'assistant' &&
        !message.turnStopped &&
        !!message.content
      );

    return latestCompletedAssistant?.id === assistantMessage.id;
  }

  beginEditCompletedPrompt(userMessage: CopilotMessage): void {
    if (
      this.isSending() ||
      this.isLoadingHistory() ||
      this.editingUserMessageId()
    ) {
      return;
    }

    if (!this.canEditCompletedUserMessage(userMessage.id)) {
      this.errorMessage.set(
        'Only the latest completed request can be edited.'
      );
      return;
    }

    this.errorMessage.set('');
    this.editingCompletedTurn.set(true);
    this.editingUserMessageId.set(userMessage.id);
    this.editedPrompt.set(userMessage.content);
  }

  submitEditedPrompt(): void {
    if (this.editingCompletedTurn()) {
      this.submitCompletedEditedPrompt();
      return;
    }

    this.submitStoppedEditedPrompt();
  }

  private submitCompletedEditedPrompt(): void {
    const userMessageId = this.editingUserMessageId();
    const editedText = this.editedPrompt().trim();

    if (
      !userMessageId ||
      !editedText ||
      this.isSending() ||
      this.isLoadingHistory()
    ) {
      return;
    }

    const messages = this.messages();

    const userMessageIndex = messages.findIndex(
      message =>
        message.id === userMessageId &&
        message.role === 'user'
    );

    if (userMessageIndex < 0) {
      this.errorMessage.set(
        'The original user message could not be found.'
      );
      return;
    }

    const assistantMessage = messages
      .slice(userMessageIndex + 1)
      .find(message =>
        message.role === 'assistant' &&
        !message.turnStopped &&
        !!message.content
      );

    if (!assistantMessage) {
      this.errorMessage.set(
        'The completed response could not be found.'
      );
      return;
    }

    const latestCompletedAssistant = [...messages]
      .reverse()
      .find(message =>
        message.role === 'assistant' &&
        !message.turnStopped &&
        !!message.content
      );

    if (latestCompletedAssistant?.id !== assistantMessage.id) {
      this.errorMessage.set(
        'Only the latest completed response can be edited.'
      );
      return;
    }

    this.errorMessage.set('');
    this.pendingApproval.set(null);
    this.activities.set([]);
    this.activityExpanded.set(true);
    this.runStopped.set(false);
    this.toolCalls.clear();

    this.isSending.set(true);
    this.activeUserMessageId = userMessageId;
    this.activeAssistantMessageId = assistantMessage.id;

    this.copilotService
      .editCompletedTurn(
        userMessageId,
        editedText
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          /*
           * Replace Version 1 visually with the new active
           * Version 2 while it is being generated.
           *
           * Version 1 remains safely stored in SQL.
           */
          this.messages.update(currentMessages =>
            currentMessages.map(message => {
              if (message.id === userMessageId) {
                return {
                  ...message,
                  content: editedText
                };
              }

              if (message.id === assistantMessage.id) {
                return {
                  ...message,
                  content: '',
                  activities: [],
                  turnStopped: false,
                  activityExpanded: true,
                  turnUserMessageId: userMessageId
                };
              }

              return message;
            })
          );

          this.editingCompletedTurn.set(false);
          this.editingUserMessageId.set(null);
          this.editedPrompt.set('');

          this.shouldAutoScroll = true;
          this.scheduleScrollToBottom(true);

          /*
           * The backend changed the active MAF branch so that
           * it now ends with the edited Version 2 user prompt.
           */
          this.activeRunSubscription =
            this.copilotService
              .runPreparedTurn()
              .subscribe({
                next: event => {
                  this.handleAgUiEvent(
                    event,
                    assistantMessage.id
                  );
                },

                error: error => {
                  console.error(
                    'AG-UI completed edit error:',
                    error
                  );

                  this.failRunningActivities();

                  this.errorMessage.set(
                    'Something went wrong while processing the edited prompt.'
                  );

                  this.activeRunSubscription = null;
                  this.activeAssistantMessageId = null;
                  this.activeUserMessageId = null;
                  this.isSending.set(false);
                },

                complete: () => {
                  this.removeEmptyAssistantMessage(
                    assistantMessage.id
                  );

                  this.activeRunSubscription = null;
                  this.activeAssistantMessageId = null;
                  this.activeUserMessageId = null;
                  this.isSending.set(false);
                }
              });
        },

        error: error => {
          console.error(
            'Failed to edit completed Copilot turn:',
            error
          );

          this.errorMessage.set(
            error?.status === 409
              ? 'Only the latest completed response can be edited.'
              : 'The completed prompt could not be edited.'
          );

          this.activeAssistantMessageId = null;
          this.activeUserMessageId = null;
          this.isSending.set(false);
        }
      });
  }

  private submitStoppedEditedPrompt(): void {
    const userMessageId = this.editingUserMessageId();
    const editedText = this.editedPrompt().trim();

    if (
      !userMessageId ||
      !editedText ||
      this.isSending() ||
      this.isLoadingHistory()
    ) {
      return;
    }

    const stoppedAssistantMessage = this.messages().find(
      message =>
        message.role === 'assistant' &&
        message.turnStopped &&
        message.turnUserMessageId === userMessageId
    );

    if (!stoppedAssistantMessage) {
      this.errorMessage.set('The interrupted response could not be found.');
      return;
    }

    if (!this.canRetryStoppedTurn(userMessageId)) {
      this.errorMessage.set('Only the latest interrupted request can be edited.');
      return;
    }

    this.errorMessage.set('');
    this.pendingApproval.set(null);
    this.activities.set([]);
    this.activityExpanded.set(true);
    this.runStopped.set(false);
    this.toolCalls.clear();

    this.isSending.set(true);
    this.activeUserMessageId = userMessageId;
    this.activeAssistantMessageId = stoppedAssistantMessage.id;

    this.copilotService.editTurn(userMessageId, editedText)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          /*
           * Stop may have been clicked while edit-turn
           * was being persisted.
           */
          if (this.runStopped()) {
            this.persistStoppedTurn(
              userMessageId,
              this.getMessageActivities(stoppedAssistantMessage.id)
            );

            this.editingUserMessageId.set(null);
            this.editedPrompt.set('');
            return;
          }

          /*
           * Update the existing user bubble.
           * The same MessageId is retained.
           */
          this.messages.update(messages =>
            messages.map(message => {
              if (message.id === userMessageId) {
                return {
                  ...message,
                  content: editedText
                };
              }

              if (message.id === stoppedAssistantMessage.id) {
                return {
                  ...message,
                  content: '',
                  activities: [],
                  turnStopped: false,
                  activityExpanded: true
                };
              }

              return message;
            })
          );

          this.editingUserMessageId.set(null);
          this.editedPrompt.set('');

          this.shouldAutoScroll = true;
          this.scheduleScrollToBottom(true);

          /*
           * The backend edited the existing MAF user
           * message and removed its stopped marker.
           */
          this.activeRunSubscription = this.copilotService.runPreparedTurn().subscribe({
            next: event => {
              this.handleAgUiEvent(event, stoppedAssistantMessage.id);
            },
            error: error => {
              console.error('AG-UI edited prompt error:', error);

              this.failRunningActivities();
              this.errorMessage.set('Something went wrong while processing the edited prompt.');
              this.activeRunSubscription = null;
              this.activeAssistantMessageId = null;
              this.activeUserMessageId = null;
              this.isSending.set(false);
            },
            complete: () => {
              this.removeEmptyAssistantMessage(stoppedAssistantMessage.id);
              this.activeRunSubscription = null;
              this.activeAssistantMessageId = null;
              this.activeUserMessageId = null;
              this.isSending.set(false);
            }
          });
        },
        error: error => {
          console.error('Failed to edit Copilot turn:', error);

          this.errorMessage.set(
            error?.status === 409
              ? 'Only the latest interrupted request can be edited.'
              : 'The interrupted prompt could not be edited.'
          );

          this.activeAssistantMessageId = null;
          this.activeUserMessageId = null;
          this.isSending.set(false);
        }
      });
  }


  private removeEmptyAssistantMessage(assistantMessageId: string): void {
    this.messages.update(messages =>
      messages.filter(message =>
        !(
          message.id === assistantMessageId &&
          message.role === 'assistant' &&
          !message.content.trim()
        )
      )
    );
  }
}
