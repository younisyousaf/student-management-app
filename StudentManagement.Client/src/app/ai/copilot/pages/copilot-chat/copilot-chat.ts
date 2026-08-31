import { Component, DestroyRef, ElementRef, ViewChild, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BaseEvent, EventType } from '@ag-ui/core';
import { forkJoin, Subscription } from 'rxjs';
import { AgUiCopilotService } from '../../services/ag-ui-copilot.service';
import { CopilotActivity, CopilotActivityStatus, CopilotApprovalRequest, CopilotBranchTurn, CopilotConversation, CopilotMessage, CopilotTurn } from '../../models/copilot.model';
import { MarkdownPipe } from '../../pipes/markdown.pipe';
import { ActivityTimeline } from '../../components/activity-timeline/activity-timeline';
import { ApprovalCard } from '../../components/approval-card/approval-card';
import { ConversationSidebar } from '../../components/conversation-sidebar/conversation-sidebar';
import { InterruptedTurnActions } from '../../components/interrupted-turn-actions/interrupted-turn-actions';
import { PromptEditor } from '../../components/prompt-editor/prompt-editor';
import { CompletedTurnActions } from '../../components/completed-turn-actions/completed-turn-actions';
import { TurnVersionNavigator } from '../../components/turn-version-navigator/turn-version-navigator';
import { LucideBot, LucideBrainCircuit, LucideSend, LucideSparkles, LucideSquare, LucideZap, LucideTrash2, LucideTriangleAlert, LucideX } from '@lucide/angular';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-copilot-chat',
  standalone: true,
  imports: [MarkdownPipe, ActivityTimeline, ApprovalCard, ConversationSidebar, InterruptedTurnActions, PromptEditor, CompletedTurnActions, TurnVersionNavigator, LucideBot, LucideBrainCircuit, LucideSend, LucideSparkles, LucideSquare, LucideZap, LucideTrash2, LucideTriangleAlert, LucideX],
  templateUrl: './copilot-chat.html',
  styleUrl: './copilot-chat.scss'
})
export class CopilotChat {
  private readonly copilotService = inject(AgUiCopilotService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly toastService = inject(ToastService);

  private notifyError(message: string): void {
    this.errorMessage.set('');
    this.toastService.error('Copilot error', message);
  }

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

  readonly loadingVersionUserMessageId = signal<string | null>(null);

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

        /*
         * Keep every persisted turn, not only stopped turns.
         *
         * This allows us to restore:
         * - completed Step Blocks
         * - stopped Step Blocks
         * - version information
         */
        const turnsByUserMessageId =
          new Map<string, CopilotTurn>(
            result.turns.map(turn => [
              turn.userMessageId,
              turn
            ])
          );

        const messages: CopilotMessage[] = [];

        /*
         * Tracks which user message the next
         * assistant response belongs to.
         */
        let currentUserMessageId: string | null = null;

        for (const historyMessage of result.history) {

          /*
           * USER MESSAGE
           */
          if (historyMessage.role === 'user') {
            currentUserMessageId = historyMessage.id;

            messages.push({
              id: historyMessage.id,
              role: historyMessage.role,
              content: historyMessage.content,
              createdAt: historyMessage.createdAt
                ? new Date(historyMessage.createdAt)
                : null
            });

            const turn =
              turnsByUserMessageId.get(
                historyMessage.id
              );

            /*
             * A stopped turn does not have a normal
             * visible assistant response in MAF history.
             *
             * Recreate its assistant presentation
             * from CopilotTurns.
             */
            if (turn?.status === 'Stopped') {
              messages.push({
                id: `stopped-${historyMessage.id}`,
                role: 'assistant',
                content: '',
                createdAt:
                  new Date(turn.updatedAt),
                activities:
                  turn.activities,
                turnStopped: true,
                activityExpanded: true,
                turnUserMessageId:
                  turn.userMessageId
              });

              currentUserMessageId = null;
            }

            continue;
          }

          /*
           * ASSISTANT MESSAGE
           */
          const turn =
            currentUserMessageId
              ? turnsByUserMessageId.get(
                currentUserMessageId
              )
              : undefined;

          messages.push({
            id: historyMessage.id,
            role: historyMessage.role,
            content: historyMessage.content,

            createdAt:
              historyMessage.createdAt
                ? new Date(historyMessage.createdAt)
                : null,

            /*
             * Restore completed Step Block.
             */
            activities:
              turn?.status === 'Completed'
                ? turn.activities
                : undefined,

            activityExpanded: false,

            /*
             * Link assistant response back
             * to its user prompt.
             */
            turnUserMessageId:
              currentUserMessageId
              ?? undefined,

            /*
             * Version information.
             *
             * If CurrentVersionNumber = 2,
             * the UI can later show:
             *
             * < 2 / 2 >
             */
            versionNumber:
              turn?.status === 'Completed'
                ? turn.currentVersionNumber
                : undefined,

            totalVersions:
              turn?.status === 'Completed'
                ? turn.currentVersionNumber
                : undefined
          });

          /*
           * User → assistant pair completed.
           */
          currentUserMessageId = null;
        }

        this.messages.set(messages);

        this.shouldAutoScroll = true;
        this.scheduleScrollToBottom(true);

        this.pendingApproval.set(
          result.pendingApproval
        );
      },

      error: error => {
        console.error(
          'Failed to load Copilot conversation:',
          error
        );

        if (this.currentThreadId() === threadId) {
          this.notifyError('The conversation could not be loaded.');
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

  showPreviousVersion(assistantMessage: CopilotMessage): void {
    this.loadBranchVersion(
      assistantMessage,
      (assistantMessage.versionNumber ?? 1) - 1
    );
  }

  showNextVersion(assistantMessage: CopilotMessage): void {
    this.loadBranchVersion(
      assistantMessage,
      (assistantMessage.versionNumber ?? 1) + 1
    );
  }

  hasCompletedResponse(
    userMessageId: string
  ): boolean {
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

    return messages
      .slice(userIndex + 1, endIndex)
      .some(message =>
        message.role === 'assistant' &&
        !message.turnStopped &&
        !!message.content
      );
  }

  private loadBranchVersion(assistantMessage: CopilotMessage, targetVersionNumber: number): void {
    const userMessageId = assistantMessage.turnUserMessageId;
    const totalVersions = assistantMessage.totalVersions ?? 1;

    if (!userMessageId || targetVersionNumber < 1 || targetVersionNumber > totalVersions || this.isSending()) {
      return;
    }

    this.loadingVersionUserMessageId.set(userMessageId);
    this.errorMessage.set('');

    this.copilotService.activateBranchForVersion(userMessageId, targetVersionNumber)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: branch => {
          this.applyBranch(branch.turns, userMessageId, totalVersions);
        },
        error: error => {
          console.error('Failed to load Copilot branch:', error);
          this.notifyError('The conversation branch could not be loaded.');
          this.loadingVersionUserMessageId.set(null);
        },
        complete: () => {
          this.loadingVersionUserMessageId.set(null);
        }
      });
  }

  private applyBranch(
    turns: CopilotBranchTurn[],
    navigatedUserMessageId: string,
    totalVersions: number
  ): void {
    const currentMessages = this.messages();

    const versionMetadata = new Map<string, { versionNumber: number; totalVersions: number }>();

    for (const message of currentMessages) {
      if (message.role !== 'assistant' || !message.turnUserMessageId) {
        continue;
      }

      versionMetadata.set(message.turnUserMessageId, {
        versionNumber: message.versionNumber ?? 1,
        totalVersions: message.totalVersions ?? 1
      });
    }

    const branchMessages: CopilotMessage[] = [];

    for (const turn of turns) {
      branchMessages.push({
        id: turn.userMessageId,
        role: 'user',
        content: turn.userContent,
        createdAt: null
      });

      const existingMetadata = versionMetadata.get(turn.userMessageId);
      const isNavigatedTurn = turn.userMessageId === navigatedUserMessageId;

      branchMessages.push({
        id: turn.assistantMessageId ?? `assistant-${turn.userMessageId}-${turn.versionNumber}`,
        role: 'assistant',
        content: turn.assistantContent,
        createdAt: null,
        activities: turn.activities,
        activityExpanded: false,
        turnStopped: turn.status === 'Stopped',
        turnUserMessageId: turn.userMessageId,
        versionNumber: turn.versionNumber,
        totalVersions: isNavigatedTurn
          ? totalVersions
          : Math.max(existingMetadata?.totalVersions ?? 1, turn.versionNumber)
      });
    }

    this.messages.set(branchMessages);
    this.shouldAutoScroll = false;
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
        const userMessageId =
          this.activeUserMessageId;

        const completedActivities =
          this.activities().map(activity => ({
            ...activity
          }));

        /*
         * Move the live activity state onto the
         * completed assistant message.
         *
         * From this point the message owns its
         * Step Block instead of the global live state.
         */
        this.messages.update(messages =>
          messages.map(message =>
            message.id === assistantMessageId
              ? {
                ...message,
                activities:
                  completedActivities,
                activityExpanded: false,
                turnStopped: false,
                turnUserMessageId:
                  userMessageId ?? undefined
              }
              : message
          )
        );

        if (userMessageId) {
          const assistantMessage =
            this.messages().find(
              message =>
                message.id ===
                assistantMessageId
            );

          this.copilotService
            .completeTurn(
              userMessageId,
              assistantMessageId,
              assistantMessage?.content ?? '',
              completedActivities
            )
            .subscribe({
              error: error => {
                console.error(
                  'Failed to persist completed Copilot turn:',
                  error
                );
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
              this.notifyError('Something went wrong while contacting the assistant.');
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
            this.notifyError('The message could not be saved before starting the assistant.');
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
        this.notifyError('Something went wrong while processing the approval.');
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
        this.toastService.success('Conversation renamed', 'The conversation title was updated.');
      },
      error: error => {
        console.error('Failed to rename Copilot conversation:', error);
        this.notifyError('The conversation could not be renamed.');
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
        this.toastService.success('Conversation deleted', `"${conversation.title}" was removed.`);
      },
      error: error => {
        console.error('Failed to delete Copilot conversation:', error);
        this.notifyError('The conversation could not be deleted.');
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
      this.notifyError('Only the latest interrupted request can be retried.');
      return;
    }

    const originalUserMessage = this.messages().find(
      item =>
        item.id === userMessageId &&
        item.role === 'user'
    );

    if (!originalUserMessage) {
      this.notifyError('The original message for this retry could not be found.');
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
              this.notifyError('Something went wrong while retrying the response.');
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

          this.notifyError(
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
      this.notifyError('Only the latest interrupted request can be edited.');
      return;
    }

    const userMessage = this.messages().find(
      item =>
        item.id === userMessageId &&
        item.role === 'user'
    );

    if (!userMessage) {
      this.notifyError('The original message could not be found.');
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


  beginEditCompletedPrompt(
    userMessage: CopilotMessage
  ): void {
    if (
      this.isSending() ||
      this.isLoadingHistory() ||
      this.editingUserMessageId()
    ) {
      return;
    }

    if (!this.hasCompletedResponse(userMessage.id)) {
      return;
    }

    this.errorMessage.set('');
    this.editingCompletedTurn.set(true);
    this.editingUserMessageId.set(
      userMessage.id
    );
    this.editedPrompt.set(
      userMessage.content
    );
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

    if (!userMessageId || !editedText || this.isSending() || this.isLoadingHistory()) {
      return;
    }

    const messages = this.messages();
    const userMessageIndex = messages.findIndex(message => message.id === userMessageId && message.role === 'user');

    if (userMessageIndex < 0) {
      this.notifyError('The original user message could not be found.');
      return;
    }

    const assistantMessage = messages
      .slice(userMessageIndex + 1)
      .find(message => message.role === 'assistant' && !message.turnStopped && !!message.content);

    if (!assistantMessage) {
      this.notifyError('The completed response could not be found.');
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

    this.copilotService.editCompletedTurn(userMessageId, editedText)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: editResult => {
          // this.turnVersions.delete(userMessageId);

          this.messages.update(currentMessages => {
            const currentUserIndex = currentMessages.findIndex(
              message => message.id === userMessageId && message.role === 'user'
            );

            if (currentUserIndex < 0) {
              return currentMessages;
            }

            const editedUserMessage: CopilotMessage = {
              ...currentMessages[currentUserIndex],
              content: editedText
            };

            const newAssistantMessage: CopilotMessage = {
              ...assistantMessage,
              content: '',
              activities: [],
              turnStopped: false,
              activityExpanded: true,
              turnUserMessageId: userMessageId,
              versionNumber: editResult.versionNumber,
              totalVersions: editResult.versionNumber
            };

            return [...currentMessages.slice(0, currentUserIndex), editedUserMessage, newAssistantMessage];
          });

          this.editingCompletedTurn.set(false);
          this.editingUserMessageId.set(null);
          this.editedPrompt.set('');
          this.shouldAutoScroll = true;
          this.scheduleScrollToBottom(true);

          this.activeRunSubscription = this.copilotService.runPreparedTurn().subscribe({
            next: event => this.handleAgUiEvent(event, assistantMessage.id),
            error: error => {
              console.error('AG-UI completed edit error:', error);
              this.failRunningActivities();
              this.notifyError('Something went wrong while processing the edited prompt.');
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
          console.error('Failed to edit completed Copilot turn:', error);
          this.notifyError(error?.error?.message ?? 'The completed prompt could not be edited.');
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
      this.notifyError('The interrupted response could not be found.');
      return;
    }

    if (!this.canRetryStoppedTurn(userMessageId)) {
      this.notifyError('Only the latest interrupted request can be edited.');
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
              this.notifyError('Something went wrong while processing the edited prompt.');
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

          this.notifyError(
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
