import { Component, DestroyRef, ElementRef, ViewChild, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BaseEvent, EventType } from '@ag-ui/core';
import { forkJoin, Subscription } from 'rxjs';
import { AgUiCopilotService } from '../../services/ag-ui-copilot.service';
import { CopilotActivity, CopilotActivityStatus, CopilotApprovalRequest, CopilotConversation, CopilotMessage } from '../../models/copilot.model';
import { MarkdownPipe } from '../../pipes/markdown.pipe';
import { ActivityTimeline } from '../../components/activity-timeline/activity-timeline';
import { ApprovalCard } from '../../components/approval-card/approval-card';
import { ConversationSidebar } from '../../components/conversation-sidebar/conversation-sidebar';
@Component({
  selector: 'app-copilot-chat',
  standalone: true,
  imports: [MarkdownPipe, ActivityTimeline, ApprovalCard, ConversationSidebar],
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
  readonly message = signal('');
  readonly messages = signal<CopilotMessage[]>([]);
  readonly isLoadingHistory = signal(false);
  readonly isSending = signal(false);
  readonly runStopped = signal(false);
  readonly errorMessage = signal('');
  readonly pendingApproval = signal<CopilotApprovalRequest | null>(
    null
  );
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
  // Delete confirmation
  readonly conversationPendingDelete = signal<CopilotConversation | null>(
    null
  );
  // Tool activity
  readonly activities = signal<CopilotActivity[]>([]);
  readonly activityExpanded = signal(false);
  private readonly toolCalls = new Map<
    string,
    {
      name: string;
      arguments: string;
    }
  >();
  constructor() {
    this.destroyRef.onDestroy(
      () => {
        if (
          this.scrollFrameId !==
          null
        ) {
          cancelAnimationFrame(
            this.scrollFrameId
          );
        }
        this.activeRunSubscription
          ?.unsubscribe();
        this.activeRunSubscription =
          null;
      }
    );
    this.copilotService
      .ensureSession();
    this.currentThreadId.set(
      this.copilotService.threadId
    );
    this.copilotService
      .conversationSaved$
      .pipe(
        takeUntilDestroyed(
          this.destroyRef
        )
      )
      .subscribe(() => {
        this.loadConversations(1);
      });
    this.loadConversations();
    this.loadConversationState();
  }
  private refreshPendingApproval(
    fallback: CopilotApprovalRequest
  ): void {
    this.pendingApproval.set(
      fallback
    );
    this.copilotService
      .getPendingApproval()
      .subscribe({
        next: approval => {
          if (approval) {
            this.pendingApproval.set(
              approval
            );
          }
        },
        error: error => {
          console.error(
            'Failed to load approval presentation:',
            error
          );
        }
      });
  }
  private startActivity(
    toolCallId: string,
    toolName: string
  ): void {
    const existing =
      this.activities().some(
        activity =>
          activity.id ===
          toolCallId
      );
    if (existing) {
      this.setActivityStatus(
        toolCallId,
        'running'
      );
      this.scheduleScrollToBottom();
      return;
    }
    this.activities.update(
      activities => [
        ...activities,
        {
          id: toolCallId,
          toolName,
          status: 'running'
        }
      ]
    );
    this.scheduleScrollToBottom();
  }
  private setActivityStatus(
    toolCallId: string,
    status: CopilotActivityStatus
  ): void {
    this.activities.update(
      activities =>
        activities.map(
          activity =>
            activity.id ===
              toolCallId
              ? {
                ...activity,
                status
              }
              : activity
        )
    );
  }
  private failRunningActivities(): void {
    this.activities.update(
      activities =>
        activities.map(
          activity =>
            activity.status ===
              'running'
              ? {
                ...activity,
                status: 'failed'
              }
              : activity
        )
    );
  }
  private stopRunningActivities(): void {
    this.activities.update(
      activities =>
        activities.map(
          activity =>
            activity.status ===
              'running'
              ? {
                ...activity,
                status: 'stopped'
              }
              : activity
        )
    );
  }
  private loadConversationState(): void {
    const threadId =
      this.copilotService.threadId;
    this.isLoadingHistory.set(
      true
    );
    this.errorMessage.set('');
    forkJoin({
      history:
        this.copilotService
          .getHistory(),
      pendingApproval:
        this.copilotService
          .getPendingApproval()
    })
      .subscribe({
        next: result => {
          if (
            this.currentThreadId() !==
            threadId
          ) {
            return;
          }
          const messages:
            CopilotMessage[] =
            result.history.map(
              message => ({
                id: message.id,
                role: message.role,
                content:
                  message.content,
                createdAt:
                  message.createdAt
                    ? new Date(
                      message.createdAt
                    )
                    : null
              })
            );
          this.messages.set(
            messages
          );
          this.shouldAutoScroll =
            true;
          this.scheduleScrollToBottom(
            true
          );
          this.pendingApproval.set(
            result.pendingApproval
          );
        },
        error: error => {
          console.error(
            'Failed to load Copilot conversation:',
            error
          );
          if (
            this.currentThreadId() ===
            threadId
          ) {
            this.errorMessage.set(
              'The conversation could not be loaded.'
            );
            this.isLoadingHistory.set(
              false
            );
          }
        },
        complete: () => {
          if (
            this.currentThreadId() ===
            threadId
          ) {
            this.isLoadingHistory.set(
              false
            );
          }
        }
      });
  }
  private loadConversations(
    pageNumber:
      number =
      this.conversationPageNumber()
  ): void {
    this.isLoadingConversations.set(
      true
    );
    this.copilotService
      .getConversations(
        pageNumber,
        this.conversationPageSize
      )
      .subscribe({
        next: result => {
          this.conversations.set(
            result.items
          );
          this.conversationPageNumber.set(
            result.pageNumber
          );
          this.conversationTotalCount.set(
            result.totalCount
          );
          this.conversationTotalPages.set(
            result.totalPages
          );
          this.conversationHasPreviousPage.set(
            result.hasPreviousPage
          );
          this.conversationHasNextPage.set(
            result.hasNextPage
          );
        },
        error: error => {
          console.error(
            'Failed to load Copilot conversations:',
            error
          );
          this.isLoadingConversations.set(
            false
          );
        },
        complete: () => {
          this.isLoadingConversations.set(
            false
          );
        }
      });
  }
  stopGeneration(): void {
    if (!this.isSending()) {
      return;
    }

    this.activeRunSubscription
      ?.unsubscribe();

    this.activeRunSubscription = null;

    /*
     * The whole agent run has been manually stopped.
     * This is independent of individual tool states.
     */
    this.runStopped.set(true);

    /*
     * If a tool is still running, mark that
     * particular tool as stopped as well.
     */
    this.stopRunningActivities();

    this.copilotService
      .stopCurrentRun();

    if (
      this.activeAssistantMessageId &&
      this.activities().length === 0
    ) {
      this.removeEmptyAssistantMessage(
        this.activeAssistantMessageId
      );
    }

    this.activeAssistantMessageId = null;

    this.isSending.set(false);
    this.errorMessage.set('');
    this.activityExpanded.set(true);

    this.shouldAutoScroll = true;

    this.scheduleScrollToBottom(true);
  }
  toggleActivity(): void {
    this.activityExpanded.update(
      expanded => !expanded
    );
    this.scheduleScrollToBottom();
  }
  startNewConversation(): void {
    if (
      this.isSending() ||
      this.isLoadingHistory()
    ) {
      return;
    }
    const threadId =
      this.copilotService
        .startNewConversation();
    this.activeRunSubscription =
      null;
    this.activeAssistantMessageId =
      null;
    this.currentThreadId.set(
      threadId
    );
    this.messages.set([]);
    this.shouldAutoScroll =
      true;
    this.pendingApproval.set(
      null
    );
    this.activities.set([]);
    this.activityExpanded.set(
      false
    );
    this.runStopped.set(false);
    this.toolCalls.clear();
    this.message.set('');
    this.errorMessage.set('');
    this.closeConversationMenu();
    this.cancelRenameConversation();
    this.conversationPendingDelete.set(
      null
    );
    this.isLoadingHistory.set(
      false
    );
  }
  openConversation(
    conversation:
      CopilotConversation
  ): void {
    if (
      this.isSending() ||
      this.isLoadingHistory()
    ) {
      return;
    }
    if (
      conversation.threadId ===
      this.currentThreadId()
    ) {
      return;
    }
    this.closeConversationMenu();
    this.cancelRenameConversation();
    this.conversationPendingDelete.set(
      null
    );
    this.activeRunSubscription =
      null;
    this.activeAssistantMessageId =
      null;
    this.copilotService
      .openConversation(
        conversation
      );
    this.currentThreadId.set(
      conversation.threadId
    );
    this.messages.set([]);
    this.pendingApproval.set(
      null
    );
    this.activities.set([]);
    this.activityExpanded.set(
      false
    );
    this.runStopped.set(false);
    this.toolCalls.clear();
    this.message.set('');
    this.errorMessage.set('');
    this.shouldAutoScroll =
      true;
    this.loadConversationState();
  }
  onMessagesScroll(
    event: Event
  ): void {
    const container =
      event.target as
      HTMLDivElement;
    const distanceFromBottom =
      container.scrollHeight -
      container.scrollTop -
      container.clientHeight;
    this.shouldAutoScroll =
      distanceFromBottom <=
      this.autoScrollThreshold;
  }
  onMessageInput(
    event: Event
  ): void {
    const input =
      event.target as
      HTMLTextAreaElement;
    this.message.set(
      input.value
    );
  }
  private scheduleScrollToBottom(
    force = false
  ): void {
    if (
      !force &&
      !this.shouldAutoScroll
    ) {
      return;
    }
    if (
      this.scrollFrameId !==
      null
    ) {
      cancelAnimationFrame(
        this.scrollFrameId
      );
    }
    this.scrollFrameId =
      requestAnimationFrame(
        () => {
          this.scrollFrameId =
            null;
          const container =
            this.messagesContainer
              ?.nativeElement;
          if (!container) {
            return;
          }
          container.scrollTop =
            container.scrollHeight;
        }
      );
  }
  private isInterruptOutcome(
    outcome: unknown
  ): outcome is {
    type: 'interrupt';
    interrupts:
    Array<{
      id: string;
      reason: string;
      message?: string;
      toolCallId?: string;
    }>;
  } {
    if (
      typeof outcome !==
      'object' ||
      outcome === null
    ) {
      return false;
    }
    const value =
      outcome as {
        type?: unknown;
        interrupts?: unknown;
      };
    return (
      value.type ===
      'interrupt' &&
      Array.isArray(
        value.interrupts
      )
    );
  }
  private handleAgUiEvent(
    event: BaseEvent,
    assistantMessageId: string
  ): void {
    /*
     * Streaming assistant response
     */
    if (
      event.type ===
      EventType.TEXT_MESSAGE_CONTENT
    ) {
      const delta =
        event['delta'];
      if (
        typeof delta ===
        'string'
      ) {
        this.messages.update(
          messages =>
            messages.map(
              message =>
                message.id ===
                  assistantMessageId
                  ? {
                    ...message,
                    content:
                      message.content +
                      delta
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
    if (
      event.type ===
      EventType.TOOL_CALL_START
    ) {
      const toolCallId =
        event['toolCallId'];
      const toolCallName =
        event['toolCallName'];
      if (
        typeof toolCallId ===
        'string' &&
        typeof toolCallName ===
        'string'
      ) {
        this.toolCalls.set(
          toolCallId,
          {
            name:
              toolCallName,
            arguments: ''
          }
        );
        this.startActivity(
          toolCallId,
          toolCallName
        );
      }
      /*
       * Remove temporary narration such as
       * "I'll look that up..." once a real
       * tool call begins.
       */
      this.messages.update(
        messages =>
          messages.map(
            message =>
              message.id ===
                assistantMessageId
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
    if (
      event.type ===
      EventType.TOOL_CALL_ARGS
    ) {
      const toolCallId =
        event['toolCallId'];
      const delta =
        event['delta'];
      if (
        typeof toolCallId ===
        'string' &&
        typeof delta ===
        'string'
      ) {
        const toolCall =
          this.toolCalls.get(
            toolCallId
          );
        if (toolCall) {
          toolCall.arguments +=
            delta;
        }
      }
    }
    /*
     * Tool execution completed
     */
    if (
      event.type ===
      EventType.TOOL_CALL_RESULT
    ) {
      const toolCallId =
        event['toolCallId'];
      if (
        typeof toolCallId ===
        'string'
      ) {
        this.setActivityStatus(
          toolCallId,
          'completed'
        );
      }
    }
    /*
     * Run completed or interrupted
     */
    if (
      event.type ===
      EventType.RUN_FINISHED
    ) {
      const outcome =
        event['outcome'];
      if (
        typeof outcome ===
        'object' &&
        outcome !== null &&
        'type' in outcome &&
        outcome.type ===
        'success'
      ) {
        this.pendingApproval.set(
          null
        );
        this.activityExpanded.set(
          false
        );
        return;
      }
      if (
        this.isInterruptOutcome(
          outcome
        )
      ) {
        const interrupt =
          outcome.interrupts[0];
        if (
          !interrupt
            ?.toolCallId
        ) {
          return;
        }
        const toolCall =
          this.toolCalls.get(
            interrupt.toolCallId
          );
        if (!toolCall) {
          return;
        }
        this.setActivityStatus(
          interrupt.toolCallId,
          'waiting'
        );
        /*
         * Keep activity visible while
         * waiting for approval.
         */
        this.activityExpanded.set(
          true
        );
        const fallbackApproval:
          CopilotApprovalRequest = {
          interruptId:
            interrupt.id,
          toolCallId:
            interrupt.toolCallId,
          toolName:
            toolCall.name,
          arguments:
            toolCall.arguments,
          message:
            interrupt.message,
          displayTitle:
            this.humanizeToolName(
              toolCall.name
            ),
          displayDetails: [],
          warning: null
        };
        this.refreshPendingApproval(
          fallbackApproval
        );
        this.scheduleScrollToBottom();
        return;
      }
      this.activityExpanded.set(
        false
      );
    }
  }
  private humanizeToolName(
    toolName: string
  ): string {
    const value =
      toolName
        .replace(
          /_/g,
          ' '
        )
        .replace(
          /([a-z])([A-Z])/g,
          '$1 $2'
        )
        .toLowerCase();
    return (
      value.charAt(0).toUpperCase() +
      value.slice(1)
    );
  }
  sendMessage(): void {
    const text =
      this.message().trim();
    if (
      !text ||
      this.isSending() ||
      this.isLoadingHistory()
    ) {
      return;
    }
    this.activities.set([]);
    this.activityExpanded.set(
      true
    );
    this.runStopped.set(false);
    const userMessage:
      CopilotMessage = {
      id: crypto.randomUUID(),
      role: 'user',
      content: text,
      createdAt: new Date()
    };
    const assistantMessage:
      CopilotMessage = {
      id: crypto.randomUUID(),
      role: 'assistant',
      content: '',
      createdAt: new Date()
    };
    this.shouldAutoScroll =
      true;
    this.messages.update(
      messages => [
        ...messages,
        userMessage,
        assistantMessage
      ]
    );
    this.scheduleScrollToBottom(
      true
    );
    this.pendingApproval.set(
      null
    );
    this.toolCalls.clear();
    this.message.set('');
    this.errorMessage.set('');
    this.closeConversationMenu();
    this.isSending.set(
      true
    );
    this.activeAssistantMessageId =
      assistantMessage.id;
    this.activeRunSubscription =
      this.copilotService
        .sendMessage(
          text
        )
        .subscribe({
          next: event => {
            this.handleAgUiEvent(
              event,
              assistantMessage.id
            );
          },
          error: error => {
            console.error(
              'AG-UI error:',
              error
            );
            this.failRunningActivities();
            this.removeEmptyAssistantMessage(
              assistantMessage.id
            );
            this.errorMessage.set(
              'Something went wrong while contacting the assistant.'
            );
            this.activeRunSubscription =
              null;
            this.activeAssistantMessageId =
              null;
            this.isSending.set(
              false
            );
          },
          complete: () => {
            this.removeEmptyAssistantMessage(
              assistantMessage.id
            );
            this.activeRunSubscription =
              null;
            this.activeAssistantMessageId =
              null;
            this.isSending.set(
              false
            );
          }
        });
  }
  respondToApproval(
    approved: boolean
  ): void {
    const approval =
      this.pendingApproval();
    if (
      !approval ||
      this.isSending()
    ) {
      return;
    }
    this.activityExpanded.set(
      true
    );
    this.pendingApproval.set(
      null
    );
    this.runStopped.set(false);
    this.setActivityStatus(
      approval.toolCallId,
      approved
        ? 'running'
        : 'rejected'
    );
    this.toolCalls.clear();
    const assistantMessage:
      CopilotMessage = {
      id: crypto.randomUUID(),
      role: 'assistant',
      content: '',
      createdAt: new Date()
    };
    this.shouldAutoScroll =
      true;
    this.messages.update(
      messages => [
        ...messages,
        assistantMessage
      ]
    );
    this.scheduleScrollToBottom(
      true
    );
    this.errorMessage.set('');
    this.isSending.set(
      true
    );
    this.activeAssistantMessageId =
      assistantMessage.id;
    this.activeRunSubscription =
      this.copilotService
        .resumeApproval(
          approval,
          approved
        )
        .subscribe({
          next: event => {
            this.handleAgUiEvent(
              event,
              assistantMessage.id
            );
          },
          error: error => {
            console.error(
              'AG-UI approval resume error:',
              error
            );
            this.setActivityStatus(
              approval.toolCallId,
              'waiting'
            );
            this.removeEmptyAssistantMessage(
              assistantMessage.id
            );
            this.pendingApproval.set(
              approval
            );
            this.errorMessage.set(
              'Something went wrong while processing the approval.'
            );
            this.activeRunSubscription =
              null;
            this.activeAssistantMessageId =
              null;
            this.isSending.set(
              false
            );
          },
          complete: () => {
            this.removeEmptyAssistantMessage(
              assistantMessage.id
            );
            this.activeRunSubscription =
              null;
            this.activeAssistantMessageId =
              null;
            this.isSending.set(
              false
            );
          }
        });
  }
  toggleConversationMenu(
    threadId: string,
    event: Event
  ): void {
    event.stopPropagation();
    this.openConversationMenuThreadId
      .update(
        current =>
          current ===
            threadId
            ? null
            : threadId
      );
  }
  closeConversationMenu(): void {
    this.openConversationMenuThreadId.set(
      null
    );
  }
  beginRenameConversation(
    conversation:
      CopilotConversation,
    event: Event
  ): void {
    event.stopPropagation();
    this.renamingConversationThreadId.set(
      conversation.threadId
    );
    this.renameConversationTitle.set(
      conversation.title
    );
    this.closeConversationMenu();
  }
  onRenameConversationInput(
    event: Event
  ): void {
    const input =
      event.target as
      HTMLInputElement;
    this.renameConversationTitle.set(
      input.value
    );
  }
  cancelRenameConversation(
    event?: Event
  ): void {
    event?.stopPropagation();
    this.renamingConversationThreadId.set(
      null
    );
    this.renameConversationTitle.set(
      ''
    );
  }
  saveRenameConversation(
    conversation:
      CopilotConversation,
    event: Event
  ): void {
    event.stopPropagation();
    const title =
      this.renameConversationTitle()
        .trim();
    if (!title) {
      return;
    }
    if (
      title ===
      conversation.title
    ) {
      this.cancelRenameConversation();
      return;
    }
    this.managingConversationThreadId.set(
      conversation.threadId
    );
    this.errorMessage.set('');
    this.copilotService
      .renameConversation(
        conversation.threadId,
        title
      )
      .subscribe({
        next:
          updatedConversation => {
            this.conversations.update(
              conversations =>
                conversations.map(
                  item =>
                    item.threadId ===
                      updatedConversation
                        .threadId
                      ? updatedConversation
                      : item
                )
            );
            this.cancelRenameConversation();
          },
        error: error => {
          console.error(
            'Failed to rename Copilot conversation:',
            error
          );
          this.errorMessage.set(
            'The conversation could not be renamed.'
          );
          this.managingConversationThreadId.set(
            null
          );
        },
        complete: () => {
          this.managingConversationThreadId.set(
            null
          );
        }
      });
  }
  requestDeleteConversation(
    conversation:
      CopilotConversation,
    event: Event
  ): void {
    event.stopPropagation();
    this.closeConversationMenu();
    this.conversationPendingDelete.set(
      conversation
    );
  }
  cancelDeleteConversation(): void {
    const conversation =
      this.conversationPendingDelete();
    if (
      conversation &&
      this.managingConversationThreadId() ===
      conversation.threadId
    ) {
      return;
    }
    this.conversationPendingDelete.set(
      null
    );
  }
  deleteConversation(): void {
    const conversation =
      this.conversationPendingDelete();
    if (!conversation) {
      return;
    }
    if (
      this.managingConversationThreadId() ===
      conversation.threadId
    ) {
      return;
    }
    this.managingConversationThreadId.set(
      conversation.threadId
    );
    this.errorMessage.set('');
    this.copilotService
      .deleteConversation(
        conversation.threadId
      )
      .subscribe({
        next: () => {
          if (
            conversation.threadId ===
            this.currentThreadId()
          ) {
            const threadId =
              this.copilotService
                .startNewConversation();
            this.activeRunSubscription =
              null;
            this.activeAssistantMessageId =
              null;
            this.currentThreadId.set(
              threadId
            );
            this.messages.set([]);
            this.pendingApproval.set(
              null
            );
            this.activities.set([]);
            this.activityExpanded.set(
              false
            );
            this.toolCalls.clear();
            this.message.set('');
            this.errorMessage.set('');
            this.shouldAutoScroll =
              true;
          }
          const remainingCount =
            Math.max(
              0,
              this.conversationTotalCount() -
              1
            );
          const remainingPages =
            remainingCount === 0
              ? 0
              : Math.ceil(
                remainingCount /
                this.conversationPageSize
              );
          const pageToLoad =
            Math.min(
              this.conversationPageNumber(),
              Math.max(
                1,
                remainingPages
              )
            );
          this.conversationPendingDelete.set(
            null
          );
          this.loadConversations(
            pageToLoad
          );
        },
        error: error => {
          console.error(
            'Failed to delete Copilot conversation:',
            error
          );
          this.errorMessage.set(
            'The conversation could not be deleted.'
          );
          this.managingConversationThreadId.set(
            null
          );
        },
        complete: () => {
          this.managingConversationThreadId.set(
            null
          );
        }
      });
  }
  previousConversationPage(): void {
    if (
      this.isLoadingConversations() ||
      !this.conversationHasPreviousPage()
    ) {
      return;
    }
    this.closeConversationMenu();
    this.cancelRenameConversation();
    this.loadConversations(
      this.conversationPageNumber() -
      1
    );
  }
  nextConversationPage(): void {
    if (
      this.isLoadingConversations() ||
      !this.conversationHasNextPage()
    ) {
      return;
    }
    this.closeConversationMenu();
    this.cancelRenameConversation();
    this.loadConversations(
      this.conversationPageNumber() +
      1
    );
  }
  private removeEmptyAssistantMessage(
    assistantMessageId: string
  ): void {
    this.messages.update(
      messages =>
        messages.filter(
          message =>
            !(
              message.id ===
              assistantMessageId &&
              message.role ===
              'assistant' &&
              !message.content.trim()
            )
        )
    );
  }
}
