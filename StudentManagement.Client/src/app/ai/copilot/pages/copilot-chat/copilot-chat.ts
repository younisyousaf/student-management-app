import { Component, inject, signal } from '@angular/core';
import { EventType, BaseEvent } from '@ag-ui/core';
import { AgUiCopilotService } from '../../services/ag-ui-copilot.service';
import { CopilotMessage, CopilotApprovalRequest } from '../../models/copilot.model';

@Component({
  selector: 'app-copilot-chat',
  standalone: true,
  imports: [],
  templateUrl: './copilot-chat.html',
  styleUrl: './copilot-chat.scss'
})
export class CopilotChat {

  private readonly copilotService = inject(AgUiCopilotService);

  readonly message = signal('');
  readonly messages = signal<CopilotMessage[]>([]);
  readonly isSending = signal(false);
  readonly errorMessage = signal('');
  readonly pendingApproval = signal<CopilotApprovalRequest | null>(null);

  private readonly toolCalls = new Map<
    string,
    {
      name: string;
      arguments: string;
    }
  >();

  onMessageInput(event: Event): void {
    const input = event.target as HTMLTextAreaElement;
    this.message.set(input.value);
  }

  private isInterruptOutcome(
    outcome: unknown
  ): outcome is {
    type: 'interrupt';
    interrupts: Array<{
      id: string;
      reason: string;
      message?: string;
      toolCallId?: string;
    }>;
  } {

    if (
      typeof outcome !== 'object' ||
      outcome === null
    ) {
      return false;
    }

    const value = outcome as {
      type?: unknown;
      interrupts?: unknown;
    };

    return (
      value.type === 'interrupt' &&
      Array.isArray(value.interrupts)
    );
  }

  private handleAgUiEvent(
    event: BaseEvent,
    assistantMessageId: string
  ): void {

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
      }
    }

    if (event.type === EventType.TOOL_CALL_START) {

      const toolCallId = event['toolCallId'];
      const toolCallName = event['toolCallName'];

      if (
        typeof toolCallId === 'string' &&
        typeof toolCallName === 'string'
      ) {

        this.toolCalls.set(toolCallId, {
          name: toolCallName,
          arguments: ''
        });
      }
    }

    if (event.type === EventType.TOOL_CALL_ARGS) {

      const toolCallId = event['toolCallId'];
      const delta = event['delta'];

      if (
        typeof toolCallId === 'string' &&
        typeof delta === 'string'
      ) {

        const toolCall = this.toolCalls.get(toolCallId);

        if (toolCall) {
          toolCall.arguments += delta;
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

        this.pendingApproval.set(null);

        return;
      }

      if (this.isInterruptOutcome(outcome)) {

        const interrupt = outcome.interrupts[0];

        if (!interrupt?.toolCallId) {
          return;
        }

        const toolCall =
          this.toolCalls.get(interrupt.toolCallId);

        if (!toolCall) {
          return;
        }

        this.pendingApproval.set({
          interruptId: interrupt.id,
          toolCallId: interrupt.toolCallId,
          toolName: toolCall.name,
          arguments: toolCall.arguments,
          message: interrupt.message
        });
      }
    }
  }

  sendMessage(): void {

    const text = this.message().trim();

    if (!text || this.isSending()) {
      return;
    }

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

    this.messages.update(messages => [
      ...messages,
      userMessage,
      assistantMessage
    ]);

    this.pendingApproval.set(null);
    this.toolCalls.clear();

    this.message.set('');
    this.errorMessage.set('');
    this.isSending.set(true);

    this.copilotService.sendMessage(text).subscribe({

      next: event => {
        this.handleAgUiEvent(
          event,
          assistantMessage.id
        );
      },

      error: error => {

        console.error('AG-UI error:', error);

        this.errorMessage.set(
          'Something went wrong while contacting the assistant.'
        );

        this.isSending.set(false);
      },

      complete: () => {

        this.removeEmptyAssistantMessage(
          assistantMessage.id
        );
        this.isSending.set(false);
      }
    });
  }

  respondToApproval(approved: boolean): void {

    const approval = this.pendingApproval();

    if (!approval || this.isSending()) {
      return;
    }

    this.pendingApproval.set(null);
    this.toolCalls.clear();

    const assistantMessage: CopilotMessage = {
      id: crypto.randomUUID(),
      role: 'assistant',
      content: '',
      createdAt: new Date()
    };

    this.messages.update(messages => [
      ...messages,
      assistantMessage
    ]);

    this.errorMessage.set('');
    this.isSending.set(true);

    this.copilotService
      .resumeApproval(approval, approved)
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

          this.errorMessage.set(
            'Something went wrong while processing the approval.'
          );

          this.isSending.set(false);
        },

        complete: () => {

          this.removeEmptyAssistantMessage(
            assistantMessage.id
          );

          this.isSending.set(false);
        }
      });
  }

  private removeEmptyAssistantMessage(
    assistantMessageId: string
  ): void {

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
