// import { Injectable } from '@angular/core';
// import { HttpAgent } from '@ag-ui/client';
// import {
//   BaseEvent,
//   EventType,
//   RunAgentInput
// } from '@ag-ui/core';
// import { Observable, tap } from 'rxjs';
// import { environment } from '../../../../environments/environment.development';
// import { CopilotApprovalRequest } from '../models/copilot.model';

// @Injectable({
//   providedIn: 'root'
// })
// export class AgUiCopilotService {

//   private readonly threadIdStorageKey =
//     'student-management-copilot-thread-id';

//   private readonly parentRunIdStorageKey =
//     'student-management-copilot-parent-run-id';

//   private readonly agent = new HttpAgent({
//     url: `${environment.apiUrl}/ag-ui/copilot`,

//     threadId: this.getOrCreateThreadId(),

//     fetch: (url, requestInit) =>
//       fetch(url, {
//         ...requestInit,
//         credentials: 'include'
//       })
//   });

//   private parentRunId =
//     sessionStorage.getItem(
//       this.parentRunIdStorageKey
//     ) ?? undefined;

//   get threadId(): string {
//     return this.agent.threadId;
//   }

//   sendMessage(
//     message: string
//   ): Observable<BaseEvent> {

//     const runId = crypto.randomUUID();

//     const input: RunAgentInput = {
//       threadId: this.agent.threadId,
//       runId,
//       parentRunId: this.parentRunId,

//       state: {},

//       messages: [
//         {
//           id: crypto.randomUUID(),
//           role: 'user',
//           content: message
//         }
//       ],

//       tools: [],
//       context: [],
//       forwardedProps: {}
//     };

//     return this.agent.run(input).pipe(
//       tap(event => {

//         if (
//           event.type === EventType.RUN_FINISHED
//         ) {
//           this.saveParentRunId(runId);
//         }

//       })
//     );
//   }

//   resumeApproval(
//     approval: CopilotApprovalRequest,
//     approved: boolean
//   ): Observable<BaseEvent> {

//     const runId = crypto.randomUUID();

//     let toolArguments: unknown = {};

//     if (approval.arguments.trim()) {
//       toolArguments =
//         JSON.parse(approval.arguments);
//     }

//     const input: RunAgentInput = {
//       threadId: this.agent.threadId,
//       runId,
//       parentRunId: this.parentRunId,

//       state: {},

//       messages: [],

//       tools: [],
//       context: [],
//       forwardedProps: {},

//       resume: [
//         {
//           interruptId: approval.interruptId,
//           status: 'resolved',

//           payload: {
//             approved,

//             toolCall: {
//               callId: approval.toolCallId,
//               name: approval.toolName,
//               arguments: toolArguments
//             }
//           }
//         }
//       ]
//     };

//     return this.agent.run(input).pipe(
//       tap(event => {

//         if (
//           event.type === EventType.RUN_FINISHED
//         ) {
//           this.saveParentRunId(runId);
//         }

//       })
//     );
//   }

//   private getOrCreateThreadId(): string {

//     const storedThreadId =
//       sessionStorage.getItem(
//         this.threadIdStorageKey
//       );

//     if (storedThreadId) {
//       return storedThreadId;
//     }

//     const threadId = crypto.randomUUID();

//     sessionStorage.setItem(
//       this.threadIdStorageKey,
//       threadId
//     );

//     return threadId;
//   }

//   private saveParentRunId(
//     runId: string
//   ): void {

//     this.parentRunId = runId;

//     sessionStorage.setItem(
//       this.parentRunIdStorageKey,
//       runId
//     );
//   }
// }

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

  private readonly threadIdStorageKey =
    'student-management-copilot-thread-id';

  private readonly parentRunIdStorageKey =
    'student-management-copilot-parent-run-id';

  private agent: HttpAgent;

  private parentRunId =
    sessionStorage.getItem(
      this.parentRunIdStorageKey
    ) ?? undefined;

  constructor() {

    const threadId =
      this.getOrCreateThreadId();

    this.agent =
      this.createAgent(threadId);
  }

  get threadId(): string {

    this.ensureSession();

    return this.agent.threadId;
  }

  /**
   * Ensures that the in-memory HttpAgent belongs
   * to the current browser Copilot session.
   *
   * This is important after logout because Angular
   * root services remain alive even though we clear
   * sessionStorage.
   */
  ensureSession(): void {

    const storedThreadId =
      sessionStorage.getItem(
        this.threadIdStorageKey
      );

    /*
     * Logout removed the browser Copilot session.
     * Create a completely new conversation.
     */
    if (!storedThreadId) {

      const newThreadId =
        crypto.randomUUID();

      sessionStorage.setItem(
        this.threadIdStorageKey,
        newThreadId
      );

      sessionStorage.removeItem(
        this.parentRunIdStorageKey
      );

      this.parentRunId =
        undefined;

      this.agent =
        this.createAgent(
          newThreadId
        );

      return;
    }

    /*
     * The browser already has a thread but the
     * in-memory agent belongs to another thread.
     */
    if (
      this.agent.threadId !==
      storedThreadId
    ) {

      this.agent =
        this.createAgent(
          storedThreadId
        );

      this.parentRunId =
        sessionStorage.getItem(
          this.parentRunIdStorageKey
        ) ?? undefined;
    }
  }

  sendMessage(
    message: string
  ): Observable<BaseEvent> {

    this.ensureSession();

    const runId =
      crypto.randomUUID();

    const input: RunAgentInput = {
      threadId: this.agent.threadId,
      runId,
      parentRunId:
        this.parentRunId,

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

    return this.agent
      .run(input)
      .pipe(
        tap(event => {

          if (
            event.type ===
            EventType.RUN_FINISHED
          ) {

            this.saveParentRunId(
              runId
            );
          }
        })
      );
  }

  resumeApproval(
    approval: CopilotApprovalRequest,
    approved: boolean
  ): Observable<BaseEvent> {

    this.ensureSession();

    const runId =
      crypto.randomUUID();

    let toolArguments: unknown =
      {};

    if (
      approval.arguments.trim()
    ) {

      toolArguments =
        JSON.parse(
          approval.arguments
        );
    }

    const input: RunAgentInput = {
      threadId:
        this.agent.threadId,

      runId,

      parentRunId:
        this.parentRunId,

      state: {},

      messages: [],

      tools: [],
      context: [],
      forwardedProps: {},

      resume: [
        {
          interruptId:
            approval.interruptId,

          status: 'resolved',

          payload: {
            approved,

            toolCall: {
              callId:
                approval.toolCallId,

              name:
                approval.toolName,

              arguments:
                toolArguments
            }
          }
        }
      ]
    };

    return this.agent
      .run(input)
      .pipe(
        tap(event => {

          if (
            event.type ===
            EventType.RUN_FINISHED
          ) {

            this.saveParentRunId(
              runId
            );
          }
        })
      );
  }

  private createAgent(
    threadId: string
  ): HttpAgent {

    return new HttpAgent({
      url:
        `${environment.apiUrl}/ag-ui/copilot`,

      threadId,

      fetch:
        (url, requestInit) =>
          fetch(
            url,
            {
              ...requestInit,
              credentials:
                'include'
            }
          )
    });
  }

  private getOrCreateThreadId():
    string {

    const storedThreadId =
      sessionStorage.getItem(
        this.threadIdStorageKey
      );

    if (storedThreadId) {
      return storedThreadId;
    }

    const threadId =
      crypto.randomUUID();

    sessionStorage.setItem(
      this.threadIdStorageKey,
      threadId
    );

    return threadId;
  }

  private saveParentRunId(
    runId: string
  ): void {

    this.parentRunId =
      runId;

    sessionStorage.setItem(
      this.parentRunIdStorageKey,
      runId
    );
  }
}
