import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BaseEvent, EventType } from '@ag-ui/core';
import { forkJoin } from 'rxjs';
import { AgUiCopilotService } from '../../services/ag-ui-copilot.service';
import { CopilotApprovalRequest, CopilotConversation, CopilotMessage, CopilotActivity, CopilotActivityStatus } from '../../models/copilot.model';
import { MarkdownPipe } from '../../pipes/markdown.pipe';
@Component({
  selector: 'app-copilot-chat',
  standalone: true,
  imports: [MarkdownPipe],
  templateUrl: './copilot-chat.html',
  styleUrl: './copilot-chat.scss'
})
export class CopilotChat {
  private readonly copilotService = inject(AgUiCopilotService);
  private readonly destroyRef = inject(DestroyRef);

  readonly message = signal('');
  readonly messages = signal<CopilotMessage[]>([]);
  readonly isLoadingHistory = signal(false);
  readonly isSending = signal(false);
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

  // Delete confirmation modal
  readonly conversationPendingDelete = signal<CopilotConversation | null>(null);

  readonly activities = signal<CopilotActivity[]>([]);
  readonly activityExpanded = signal(false);

  private readonly toolCalls =
    new Map<
      string,
      {
        name: string;
        arguments: string;
      }
    >();

  constructor() {
    this.copilotService.ensureSession();
    this.currentThreadId.set(this.copilotService.threadId);
    this.copilotService.conversationSaved$.pipe(
      takeUntilDestroyed(
        this.destroyRef
      ))
      .subscribe(() => {
        /*
         * A completed Copilot run changes
         * UpdatedAt, so reload page 1
         * from the server.
         */
        this.loadConversations(1);
      });
    this.loadConversations();
    this.loadConversationState();
  }

  private refreshPendingApproval(
    fallback: CopilotApprovalRequest
  ): void {
    this.pendingApproval.set(fallback);

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

  private startActivity(toolCallId: string, toolName: string): void {
    const existing = this.activities().some(
      activity => activity.id === toolCallId
    );

    if (existing) {
      this.setActivityStatus(toolCallId, 'running');
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
  }

  private setActivityStatus(
    toolCallId: string,
    status: CopilotActivityStatus
  ): void {
    this.activities.update(
      activities =>
        activities.map(
          activity =>
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
    this.activities.update(
      activities =>
        activities.map(
          activity =>
            activity.status === 'running'
              ? {
                ...activity,
                status: 'failed'
              }
              : activity
        )
    );
  }

  private loadConversationState(): void {
    const threadId = this.copilotService.threadId;
    this.isLoadingHistory.set(true);
    this.errorMessage.set('');
    forkJoin({
      history:
        this.copilotService.getHistory(),
      pendingApproval:
        this.copilotService.getPendingApproval()
    })
      .subscribe({
        next: result => {
          /*
           * Ignore stale responses if
           * the user switched conversation
           * while this request was running.
           */
          if (this.currentThreadId() !== threadId) {
            return;
          }
          const messages: CopilotMessage[] =
            result.history.map(
              message => ({
                id: message.id,
                role: message.role,
                content: message.content,
                createdAt: message.createdAt ? new Date(message.createdAt) : null
              })
            );
          this.messages.set(messages);
          this.pendingApproval.set(result.pendingApproval);
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
          this.conversations.set(result.items);
          this.conversationPageNumber.set(result.pageNumber);
          this.conversationTotalCount.set(result.totalCount);
          this.conversationTotalPages.set(result.totalPages);
          this.conversationHasPreviousPage.set(result.hasPreviousPage);
          this.conversationHasNextPage.set(result.hasNextPage);
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

  toggleActivity(): void {
    this.activityExpanded.update(
      expanded => !expanded
    );
  }

  startNewConversation(): void {
    if (this.isSending() || this.isLoadingHistory()) {
      return;
    }
    const threadId = this.copilotService.startNewConversation();
    this.currentThreadId.set(threadId);
    this.messages.set([]);
    this.pendingApproval.set(null);
    this.toolCalls.clear();
    this.message.set('');
    this.errorMessage.set('');
    this.closeConversationMenu();
    this.cancelRenameConversation();
    this.conversationPendingDelete.set(null);
    this.isLoadingHistory.set(false);
    this.activities.set([]);
    this.activityExpanded.set(false);
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
    this.copilotService.openConversation(conversation);
    this.currentThreadId.set(conversation.threadId);
    this.messages.set([]);
    this.pendingApproval.set(null);
    this.toolCalls.clear();
    this.message.set('');
    this.errorMessage.set('');
    this.loadConversationState();
    this.activities.set([]);
    this.activityExpanded.set(false);
  }
  onMessageInput(event: Event): void {
    const input = event.target as HTMLTextAreaElement;
    this.message.set(input.value);
  }
  private isInterruptOutcome(outcome: unknown): outcome is {
    type: 'interrupt';
    interrupts:
    Array<{
      id: string;
      reason: string;
      message?: string;
      toolCallId?: string;
    }>;
  } {
    if (typeof outcome !== 'object' || outcome === null) {
      return false;
    }
    const value =
      outcome as {
        type?: unknown;
        interrupts?: unknown;
      };
    return (
      value.type === 'interrupt' &&
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
     * Streaming assistant text
     */
    if (
      event.type ===
      EventType.TEXT_MESSAGE_CONTENT
    ) {
      const delta =
        event['delta'];
      if (
        typeof delta === 'string'
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
      }
    }
    /*
     * Tool call starts
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
        this.toolCalls.set(toolCallId,
          {
            name: toolCallName,
            arguments: ''
          }
        );
        this.startActivity(toolCallId, toolCallName);
      }
      this.messages.update(
        messages =>
          messages.map(
            message =>
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
    if (event.type === EventType.TOOL_CALL_RESULT) {
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
     * Run completed / interrupted
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
        return;
      }
      if (this.isInterruptOutcome(outcome)) {
        const interrupt =
          outcome.interrupts[0];
        if (
          !interrupt?.toolCallId
        ) {
          return;
        }
        const toolCall = this.toolCalls.get(interrupt.toolCallId);
        this.setActivityStatus(interrupt.toolCallId, 'waiting');
        if (!toolCall) {
          return;
        }
        this.setActivityStatus(
          interrupt.toolCallId,
          'waiting'
        );

        this.activityExpanded.set(true);

        const fallbackApproval:
          CopilotApprovalRequest = {
          interruptId: interrupt.id,
          toolCallId: interrupt.toolCallId,
          toolName: toolCall.name,
          arguments: toolCall.arguments,
          message: interrupt.message,
          displayTitle: this.humanizeToolName(
            toolCall.name
          ),
          displayDetails: [],
          warning: null
        };

        this.refreshPendingApproval(
          fallbackApproval
        );
      }
      this.activityExpanded.set(false);
    }
  }

  activityText(
    activity: CopilotActivity
  ): string {
    const labels: Record<
      string,
      {
        running: string;
        completed: string;
      }
    > = {
      GetStudentById: {
        running: 'Looking up student...',
        completed: 'Student lookup completed'
      },
      GetStudentByRollNumber: {
        running: 'Looking up student...',
        completed: 'Student lookup completed'
      },
      SearchStudentsByName: {
        running: 'Searching student records...',
        completed: 'Student search completed'
      },
      GetCourseById: {
        running: 'Looking up course...',
        completed: 'Course lookup completed'
      },
      GetCourseByCode: {
        running: 'Looking up course...',
        completed: 'Course lookup completed'
      },
      SearchCoursesByName: {
        running: 'Searching courses...',
        completed: 'Course search completed'
      },
      GetAllCourses: {
        running: 'Retrieving courses...',
        completed: 'Courses retrieved'
      },
      GetEnrollmentById: {
        running: 'Looking up enrollment...',
        completed: 'Enrollment lookup completed'
      },
      GetEnrollmentsByStudent: {
        running: 'Checking student enrollments...',
        completed: 'Student enrollments checked'
      },
      GetEnrollmentForStudentCourse: {
        running: 'Checking existing enrollment...',
        completed: 'Existing enrollment checked'
      },
      GetEnrollmentsByCourse: {
        running: 'Retrieving course enrollments...',
        completed: 'Course enrollments retrieved'
      },
      GetAttendanceById: {
        running: 'Looking up attendance record...',
        completed: 'Attendance record retrieved'
      },
      GetAttendanceForStudent: {
        running: 'Reviewing student attendance...',
        completed: 'Student attendance retrieved'
      },
      GetAttendanceForCourseOnDate: {
        running: 'Checking course attendance...',
        completed: 'Course attendance retrieved'
      },
      GetAttendanceSummaryForStudent: {
        running: 'Calculating attendance summary...',
        completed: 'Attendance summary calculated'
      },
      GetFeeById: {
        running: 'Looking up fee record...',
        completed: 'Fee record retrieved'
      },
      GetFeeStatement: {
        running: 'Checking fee statement...',
        completed: 'Fee statement retrieved'
      },
      GetFeesForStudent: {
        running: 'Reviewing student fees...',
        completed: 'Student fees retrieved'
      },
      SearchInstitutionalKnowledge: {
        running: 'Searching institutional knowledge...',
        completed: 'Institutional knowledge searched'
      },
      load_skill: {
        running: 'Loading task guidance...',
        completed: 'Task guidance loaded'
      },
      read_skill_resource: {
        running: 'Reading skill resource...',
        completed: 'Skill resource read'
      },
      run_skill_script: {
        running: 'Running skill...',
        completed: 'Skill completed'
      },
      create_student: {
        running: 'Creating student...',
        completed: 'Student created'
      },
      create_course: {
        running: 'Creating course...',
        completed: 'Course created'
      },
      enroll_student: {
        running: 'Enrolling student...',
        completed: 'Student enrolled'
      },
      drop_course: {
        running: 'Dropping enrollment...',
        completed: 'Enrollment dropped'
      },
      complete_course: {
        running: 'Completing enrollment...',
        completed: 'Enrollment completed'
      },
      mark_attendance: {
        running: 'Recording attendance...',
        completed: 'Attendance recorded'
      },
      mark_attendance_today: {
        running: 'Recording today\'s attendance...',
        completed: 'Today\'s attendance recorded'
      },
      update_attendance: {
        running: 'Updating attendance...',
        completed: 'Attendance updated'
      },
      process_student_payment: {
        running: 'Processing payment...',
        completed: 'Payment recorded'
      },
      update_student_profile: {
        running: 'Updating student profile...',
        completed: 'Student profile updated'
      },
      remove_student: {
        running: 'Removing student...',
        completed: 'Student removed'
      },
      update_course_details: {
        running: 'Updating course...',
        completed: 'Course updated'
      },
      update_course_pricing: {
        running: 'Updating course pricing...',
        completed: 'Course pricing updated'
      },
      remove_course: {
        running: 'Removing course...',
        completed: 'Course removed'
      },
      GetStudentsBelowAttendanceThreshold: {
        running: 'Finding students with low attendance...',
        completed: 'Low-attendance report completed'
      },

      GetStudentsWithOutstandingFees: {
        running: 'Checking outstanding student fees...',
        completed: 'Outstanding-fee report completed'
      },

      GetCourseAttendanceSummary: {
        running: 'Calculating course attendance...',
        completed: 'Course attendance summary calculated'
      },
      GetStudentsWithNoAttendanceRecords: {
        running: 'Finding students without attendance records...',
        completed: 'No-attendance report completed'
      },

      GetStudentsWithNoActiveEnrollment: {
        running: 'Checking active student enrollments...',
        completed: 'Enrollment-status report completed'
      },

      GetInstitutionFeeSummary: {
        running: 'Calculating institution fee summary...',
        completed: 'Institution fee summary calculated'
      },
    };

    const label =
      labels[activity.toolName];

    if (
      activity.status ===
      'running'
    ) {
      return label?.running ??
        `Running ${this.humanizeToolName(
          activity.toolName
        )}...`;
    }

    if (
      activity.status ===
      'completed'
    ) {
      return label?.completed ??
        `${this.humanizeToolName(
          activity.toolName
        )} completed`;
    }

    if (
      activity.status ===
      'waiting'
    ) {
      return `Waiting for approval: ${this.humanizeToolName(
        activity.toolName
      )
        }`;
    }

    if (
      activity.status ===
      'rejected'
    ) {
      return `${this.humanizeToolName(
        activity.toolName
      )
        } was rejected`;
    }

    return `${this.humanizeToolName(
      activity.toolName
    )
      } failed`;
  }

  private humanizeToolName(
    toolName: string
  ): string {
    const value =
      toolName
        .replace(/_/g, ' ')
        .replace(
          /([a-z])([A-Z])/g,
          '$1 $2'
        )
        .toLowerCase();

    return value.charAt(0)
      .toUpperCase() +
      value.slice(1);
  }

  activityStatusSymbol(
    status: CopilotActivityStatus
  ): string {
    switch (status) {
      case 'completed': return '✓';
      case 'waiting': return '○';
      case 'rejected': return '×';
      case 'failed': return '!';
      default: return '●';
    }
  }

  sendMessage(): void {
    const text = this.message().trim();
    if (!text || this.isSending() || this.isLoadingHistory()) {
      return;
    }
    this.activities.set([]);
    this.activityExpanded.set(true);
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
      createdAt:
        new Date()
    };
    this.messages.update(
      messages => [
        ...messages,
        userMessage,
        assistantMessage
      ]
    );
    this.pendingApproval.set(null);
    this.toolCalls.clear();
    this.message.set('');
    this.errorMessage.set('');
    this.closeConversationMenu();
    this.isSending.set(true);
    this.copilotService.sendMessage(text).subscribe({
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
        this.removeEmptyAssistantMessage(assistantMessage.id);
        this.errorMessage.set('Something went wrong while contacting the assistant.');
        this.isSending.set(false);
      },
      complete: () => {
        this.removeEmptyAssistantMessage(assistantMessage.id);
        this.isSending.set(false);
      }
    });
  }
  respondToApproval(
    approved: boolean
  ): void {
    const approval = this.pendingApproval();
    if (!approval || this.isSending()) {
      return;
    }
    this.activityExpanded.set(true);
    this.pendingApproval.set(null);
    this.setActivityStatus(approval.toolCallId, approved ? 'running' : 'rejected');
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
    this.copilotService.resumeApproval(approval, approved)
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
          this.setActivityStatus(approval.toolCallId, 'waiting');
          this.removeEmptyAssistantMessage(assistantMessage.id);
          this.pendingApproval.set(approval);
          this.errorMessage.set('Something went wrong while processing the approval.');
          this.isSending.set(false);
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

  toggleConversationMenu(
    threadId: string,
    event: Event
  ): void {
    event.stopPropagation();
    this.openConversationMenuThreadId
      .update(
        current =>
          current === threadId
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
                      updatedConversation.threadId
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
  /*
   * Open custom delete modal.
   */
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
  /*
   * Close custom delete modal.
   */
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
  /*
   * Performs the actual deletion after
   * the user confirms through our modal.
   */
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
          /*
           * If the current conversation
           * was deleted, create a fresh
           * blank conversation.
           */
          if (
            conversation.threadId ===
            this.currentThreadId()
          ) {
            const threadId = this.copilotService.startNewConversation();
            this.currentThreadId.set(threadId);
            this.messages.set([]);
            this.pendingApproval.set(null);
            this.toolCalls.clear();
            this.message.set('');
            this.errorMessage.set('');
          }
          /*
           * Work out which page is still
           * valid after the deletion.
           */
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
          /*
           * Close modal after success.
           */
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
