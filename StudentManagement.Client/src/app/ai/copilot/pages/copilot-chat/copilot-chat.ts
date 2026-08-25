import { Component, inject, signal, DestroyRef } from '@angular/core';
import {
  takeUntilDestroyed
} from '@angular/core/rxjs-interop';
import { EventType, BaseEvent } from '@ag-ui/core';
import { AgUiCopilotService } from '../../services/ag-ui-copilot.service';
import { CopilotMessage, CopilotApprovalRequest, CopilotConversation } from '../../models/copilot.model';
import { forkJoin } from 'rxjs';
@Component({
  selector: 'app-copilot-chat',
  standalone: true,
  imports: [],
  templateUrl: './copilot-chat.html',
  styleUrl: './copilot-chat.scss'
})
export class CopilotChat {

  private readonly copilotService = inject(AgUiCopilotService);
  /*
   * This belongs here because CopilotMessage[]
   * is UI state owned by this component.
   */

  readonly message = signal('');
  readonly messages = signal<CopilotMessage[]>([]);
  readonly isLoadingHistory = signal(false);
  readonly isSending = signal(false);
  readonly errorMessage = signal('');
  readonly pendingApproval = signal<CopilotApprovalRequest | null>(null);
  readonly conversations = signal<CopilotConversation[]>([]);
  readonly isLoadingConversations = signal(false);
  readonly currentThreadId = signal('');

  private readonly toolCalls = new Map<string,
    {
      name: string;
      arguments: string;
    }
  >();

  private readonly destroyRef =
    inject(DestroyRef);

  constructor() {

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
      .subscribe(
        conversation => {

          this.updateConversationList(
            conversation
          );
        }
      );

    this.loadConversations();

    this.loadConversationState();
  }

  private loadConversationState(): void {

    const threadId =
      this.copilotService.threadId;

    this.isLoadingHistory.set(true);

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

          /*
           * Ignore an old HTTP response if the
           * user switched conversations while
           * requests were in progress.
           */
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
                content: message.content,

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

  private loadConversations(): void {

    this.isLoadingConversations.set(
      true
    );

    this.copilotService
      .getConversations()
      .subscribe({

        next: conversations => {

          this.conversations.set(
            conversations
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

  private updateConversationList(
    conversation: CopilotConversation
  ): void {

    this.conversations.update(
      conversations => {

        const remaining =
          conversations.filter(
            item =>
              item.threadId !==
              conversation.threadId
          );

        return [
          conversation,
          ...remaining
        ];
      }
    );
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

    this.currentThreadId.set(
      threadId
    );

    this.messages.set([]);

    this.pendingApproval.set(
      null
    );

    this.toolCalls.clear();

    this.message.set('');

    this.errorMessage.set('');

    this.isLoadingHistory.set(
      false
    );
  }

  onMessageInput(event: Event): void {
    const input = event.target as HTMLTextAreaElement;
    this.message.set(input.value);
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

    return (value.type === 'interrupt' &&
      Array.isArray(
        value.interrupts
      )
    );
  }

  openConversation(
    conversation: CopilotConversation
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

    this.toolCalls.clear();

    this.message.set('');

    this.errorMessage.set('');

    this.loadConversationState();
  }

  private handleAgUiEvent(
    event: BaseEvent,
    assistantMessageId: string
  ): void {

    if (event.type === EventType.TEXT_MESSAGE_CONTENT) {
      const delta = event['delta'];
      if (typeof delta === 'string') {
        this.messages.update(
          messages => messages.map(
            message => message.id === assistantMessageId ?
              {
                ...message,
                content:
                  message.content +
                  delta
              }
              : message
          )
        );
      }
    }

    if (event.type === EventType.TOOL_CALL_START) {
      const toolCallId = event['toolCallId'];
      const toolCallName = event['toolCallName'];

      if (
        typeof toolCallId === 'string' &&
        typeof toolCallName === 'string'
      ) {

        this.toolCalls.set(
          toolCallId,
          {
            name: toolCallName,
            arguments: ''
          }
        );
      }
    }

    if (event.type === EventType.TOOL_CALL_ARGS) {
      const toolCallId = event['toolCallId'];
      const delta = event['delta'];

      if (
        typeof toolCallId === 'string' &&
        typeof delta === 'string'
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

    if (event.type === EventType.RUN_FINISHED) {
      const outcome = event['outcome'];
      if (
        typeof outcome === 'object' &&
        outcome !== null &&
        'type' in outcome &&
        outcome.type === 'success'
      ) {

        this.pendingApproval.set(
          null
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
          !interrupt?.toolCallId
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

        this.pendingApproval.set({
          interruptId:
            interrupt.id,

          toolCallId:
            interrupt.toolCallId,

          toolName:
            toolCall.name,

          arguments:
            toolCall.arguments,

          message:
            interrupt.message
        });
      }
    }
  }

  sendMessage(): void {

    const text =
      this.message().trim();

    if (!text || this.isSending() || this.isLoadingHistory()) {
      return;
    }

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

    this.messages.update(
      messages => [
        ...messages,
        userMessage,
        assistantMessage
      ]
    );



    this.pendingApproval.set(
      null
    );

    this.toolCalls.clear();

    this.message.set('');

    this.errorMessage.set('');

    this.isSending.set(true);

    this.copilotService
      .sendMessage(text)
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

          this.removeEmptyAssistantMessage(
            assistantMessage.id
          );

          this.errorMessage.set(
            'Something went wrong while contacting the assistant.'
          );

          this.isSending.set(
            false
          );
        },

        complete: () => {

          this.removeEmptyAssistantMessage(
            assistantMessage.id
          );

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

    this.pendingApproval.set(
      null
    );

    this.toolCalls.clear();

    const assistantMessage:
      CopilotMessage = {

      id: crypto.randomUUID(),
      role: 'assistant',
      content: '',
      createdAt: new Date()
    };

    this.messages.update(
      messages => [
        ...messages,
        assistantMessage
      ]
    );



    this.errorMessage.set('');

    this.isSending.set(true);

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

          this.removeEmptyAssistantMessage(
            assistantMessage.id
          );

          this.pendingApproval.set(
            approval
          );

          this.errorMessage.set(
            'Something went wrong while processing the approval.'
          );

          this.isSending.set(
            false
          );
        },

        complete: () => {

          this.removeEmptyAssistantMessage(
            assistantMessage.id
          );

          this.isSending.set(
            false
          );
        }

      });
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
