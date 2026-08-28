import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { HttpAgent } from '@ag-ui/client';
import { BaseEvent, EventType, RunAgentInput } from '@ag-ui/core';
import { Observable, Subject, tap } from 'rxjs';
import { environment } from '../../../../environments/environment.development';
import { CopilotApprovalRequest, CopilotHistoryMessage, CopilotConversation, CopilotTurn, CopilotActivity } from '../models/copilot.model';
import { PaginatedResult } from '../../../shared/models/paginated-result.model';

@Injectable({
  providedIn: 'root'
})
export class AgUiCopilotService {
  private readonly threadIdStorageKey = 'student-management-copilot-thread-id';
  private readonly parentRunIdStorageKey = 'student-management-copilot-parent-run-id';
  private agent: HttpAgent;
  private readonly http = inject(HttpClient);
  private parentRunId = sessionStorage.getItem(this.parentRunIdStorageKey) ?? undefined;

  private readonly conversationSavedSubject = new Subject<CopilotConversation>();
  readonly conversationSaved$ = this.conversationSavedSubject.asObservable();

  constructor() {
    const threadId = this.getOrCreateThreadId();
    this.agent = this.createAgent(threadId);
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
    const storedThreadId = sessionStorage.getItem(this.threadIdStorageKey);

    /*
     * Logout removed the browser Copilot session.
     * Create a completely new conversation.
     */
    if (!storedThreadId) {
      const newThreadId = crypto.randomUUID();

      sessionStorage.setItem(this.threadIdStorageKey, newThreadId);
      sessionStorage.removeItem(this.parentRunIdStorageKey);

      this.parentRunId = undefined;
      this.agent = this.createAgent(newThreadId);
      return;
    }

    /*
     * The browser already has a thread but the
     * in-memory agent belongs to another thread.
     */
    if (this.agent.threadId !== storedThreadId) {
      this.agent = this.createAgent(storedThreadId);
      this.parentRunId = sessionStorage.getItem(this.parentRunIdStorageKey) ?? undefined;
    }
  }

  /*
   * The user message has already been persisted
   * through prepareTurn().
   *
   * Therefore this AG-UI run intentionally sends
   * no new user message.
   */
  runPreparedTurn(): Observable<BaseEvent> {
    this.ensureSession();

    const runId = crypto.randomUUID();

    const input: RunAgentInput = {
      threadId: this.agent.threadId,
      runId,
      parentRunId: this.parentRunId,
      state: {},
      messages: [],
      tools: [],
      context: [],
      forwardedProps: {}
    };

    return this.agent.run(input).pipe(
      tap(event => {
        if (event.type === EventType.RUN_FINISHED) {
          this.saveParentRunId(runId);
          this.saveConversationRun(runId);
        }
      })
    );
  }

  resumeApproval(approval: CopilotApprovalRequest, approved: boolean): Observable<BaseEvent> {
    this.ensureSession();

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
          this.saveParentRunId(runId);
          this.saveConversationRun(runId);
        }
      })
    );
  }

  stopCurrentRun(): void {
    this.ensureSession();

    const threadId = this.agent.threadId;
    this.agent.abortRun();

    /*
     * We call HttpAgent.run(...) directly.
     * The aborted agent must not be reused
     * because its AbortController is now aborted.
     */
    this.agent = this.createAgent(threadId);
  }

  prepareTurn(
    messageId: string,
    message: string
  ): Observable<CopilotConversation> {
    this.ensureSession();

    return this.http.post<CopilotConversation>(
      `${environment.apiUrl}/ag-ui/copilot/conversations/prepare-turn`,
      {
        threadId: this.agent.threadId,
        messageId,
        message
      },
      {
        withCredentials: true
      }
    ).pipe(
      tap(conversation => {
        /*
         * Refresh the sidebar immediately.
         * We do not wait for the AI run to finish.
         */
        this.conversationSavedSubject.next(conversation);
      })
    );
  }

  private createAgent(threadId: string): HttpAgent {
    return new HttpAgent({
      url: `${environment.apiUrl}/ag-ui/copilot`,
      threadId,
      fetch: (url, requestInit) =>
        fetch(url, {
          ...requestInit,
          credentials: 'include'
        })
    });
  }

  private getOrCreateThreadId(): string {
    const storedThreadId = sessionStorage.getItem(this.threadIdStorageKey);

    if (storedThreadId) {
      return storedThreadId;
    }

    const threadId = crypto.randomUUID();
    sessionStorage.setItem(this.threadIdStorageKey, threadId);

    return threadId;
  }

  private saveParentRunId(runId: string): void {
    this.parentRunId = runId;
    sessionStorage.setItem(this.parentRunIdStorageKey, runId);
  }

  private saveConversationRun(
    runId: string,
    title?: string
  ): void {
    this.http.post<CopilotConversation>(
      `${environment.apiUrl}/ag-ui/copilot/conversations/run`,
      {
        threadId: this.agent.threadId,
        runId,
        title: title ?? null
      },
      {
        withCredentials: true
      }
    ).subscribe({
      next: conversation => {
        this.conversationSavedSubject.next(conversation);
      },
      error: error => {
        console.error('Failed to save Copilot conversation metadata:', error);
      }
    });
  }

  getHistory(): Observable<CopilotHistoryMessage[]> {
    this.ensureSession();

    const threadId = encodeURIComponent(this.agent.threadId);

    return this.http.get<CopilotHistoryMessage[]>(
      `${environment.apiUrl}/ag-ui/copilot/history/${threadId}`,
      {
        withCredentials: true
      }
    );
  }

  getPendingApproval(): Observable<CopilotApprovalRequest | null> {
    this.ensureSession();

    const threadId = encodeURIComponent(this.agent.threadId);

    return this.http.get<CopilotApprovalRequest | null>(
      `${environment.apiUrl}/ag-ui/copilot/pending-approval/${threadId}`,
      {
        withCredentials: true
      }
    );
  }

  getConversations(
    pageNumber: number,
    pageSize: number
  ): Observable<PaginatedResult<CopilotConversation>> {
    return this.http.get<PaginatedResult<CopilotConversation>>(
      `${environment.apiUrl}/ag-ui/copilot/conversations`,
      {
        params: {
          pageNumber,
          pageSize
        },
        withCredentials: true
      }
    );
  }

  startNewConversation(): string {
    const threadId = crypto.randomUUID();

    sessionStorage.setItem(this.threadIdStorageKey, threadId);
    sessionStorage.removeItem(this.parentRunIdStorageKey);

    this.parentRunId = undefined;
    this.agent = this.createAgent(threadId);

    return threadId;
  }

  openConversation(conversation: CopilotConversation): void {
    sessionStorage.setItem(this.threadIdStorageKey, conversation.threadId);

    this.agent = this.createAgent(conversation.threadId);

    if (conversation.lastRunId) {
      this.parentRunId = conversation.lastRunId;
      sessionStorage.setItem(this.parentRunIdStorageKey, conversation.lastRunId);
    } else {
      this.parentRunId = undefined;
      sessionStorage.removeItem(this.parentRunIdStorageKey);
    }
  }

  renameConversation(
    threadId: string,
    title: string
  ): Observable<CopilotConversation> {
    return this.http.patch<CopilotConversation>(
      `${environment.apiUrl}/ag-ui/copilot/conversations/${encodeURIComponent(threadId)}/title`,
      {
        title
      },
      {
        withCredentials: true
      }
    );
  }

  deleteConversation(threadId: string): Observable<void> {
    return this.http.delete<void>(
      `${environment.apiUrl}/ag-ui/copilot/conversations/${encodeURIComponent(threadId)}`,
      {
        withCredentials: true
      }
    );
  }

  stopTurn(
    userMessageId: string,
    activities: CopilotActivity[]
  ): Observable<CopilotTurn> {
    this.ensureSession();
    const threadId = encodeURIComponent(this.agent.threadId);

    return this.http.post<CopilotTurn>(
      `${environment.apiUrl}/ag-ui/copilot/conversations/${threadId}/stop-turn`,
      {
        userMessageId,
        activities
      },
      {
        withCredentials: true
      }
    );
  }

  getTurns(): Observable<CopilotTurn[]> {
    this.ensureSession();
    const threadId = encodeURIComponent(this.agent.threadId);

    return this.http.get<CopilotTurn[]>(
      `${environment.apiUrl}/ag-ui/copilot/conversations/${threadId}/turns`,
      {
        withCredentials: true
      }
    );
  }

  completeTurn(userMessageId: string): Observable<void> {
    this.ensureSession();

    const threadId = encodeURIComponent(this.agent.threadId);

    return this.http.post<void>(
      `${environment.apiUrl}/ag-ui/copilot/conversations/${threadId}/complete-turn`,
      {
        userMessageId
      },
      {
        withCredentials: true
      }
    );
  }

}
