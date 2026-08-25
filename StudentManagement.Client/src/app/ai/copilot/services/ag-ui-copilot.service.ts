import { Injectable } from '@angular/core';
import { HttpAgent } from '@ag-ui/client';
import {
  BaseEvent,
  EventType,
  RunAgentInput
} from '@ag-ui/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../../environments/environment.development';
import { CopilotApprovalRequest } from '../models/copilot.model';

@Injectable({
  providedIn: 'root'
})
export class AgUiCopilotService {

  private readonly agent = new HttpAgent({
    url: `${environment.apiUrl}/ag-ui/copilot`,

    fetch: (url, requestInit) =>
      fetch(url, {
        ...requestInit,
        credentials: 'include'
      })
  });

  private parentRunId?: string;

  get threadId(): string {
    return this.agent.threadId;
  }

  sendMessage(message: string): Observable<BaseEvent> {

    const runId = crypto.randomUUID();

    const input: RunAgentInput = {
      threadId: this.agent.threadId,
      runId,
      parentRunId: this.parentRunId,
      state: {},

      messages: [
        {
          id: crypto.randomUUID(),
          role: 'user',
          content: message
        }
      ],

      tools: [],
      context: [],
      forwardedProps: {}
    };

    return this.agent.run(input).pipe(
      tap(event => {
        if (event.type === EventType.RUN_FINISHED) {
          this.parentRunId = runId;
        }
      })
    );
  }

  resumeApproval(
    approval: CopilotApprovalRequest,
    approved: boolean
  ): Observable<BaseEvent> {

    const runId = crypto.randomUUID();

    let toolArguments: unknown = {};

    if (approval.arguments.trim()) {
      toolArguments = JSON.parse(approval.arguments);
    }

    const input: RunAgentInput = {
      threadId: this.agent.threadId,
      runId,
      parentRunId: this.parentRunId,
      state: {},

      messages: [],

      tools: [],
      context: [],
      forwardedProps: {},

      resume: [
        {
          interruptId: approval.interruptId,
          status: 'resolved',

          payload: {
            approved,

            toolCall: {
              callId: approval.toolCallId,
              name: approval.toolName,
              arguments: toolArguments
            }
          }
        }
      ]
    };

    return this.agent.run(input).pipe(
      tap(event => {
        if (event.type === EventType.RUN_FINISHED) {
          this.parentRunId = runId;
        }
      })
    );
  }
}
