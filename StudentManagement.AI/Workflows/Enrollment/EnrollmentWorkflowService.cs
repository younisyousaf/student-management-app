using Microsoft.Agents.AI.Workflows;
using StudentManagement.AI.Workflows.Enrollment.Executors;
using StudentManagement.AI.Workflows.Enrollment.Models;
using StudentManagement.Core.Enums;
using StudentManagement.Core.Interfaces;

namespace StudentManagement.AI.Workflows.Enrollment;

public sealed class EnrollmentWorkflowService
{
    private readonly Workflow _workflow;
    private readonly EnrollmentWorkflowCheckpointStore _checkpointStore;
    private readonly IEnrollmentWorkflowRecordStore _recordStore;
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentWorkflowService(
    ValidateStudentExecutor validateStudent,
    ValidateCourseExecutor validateCourse,
    CheckExistingEnrollmentExecutor checkExistingEnrollment,
    EnrollmentRejectedExecutor enrollmentRejected,
    PrepareEnrollmentApprovalExecutor prepareApproval,
    EnrollmentApprovalRejectedExecutor approvalRejected,
    EnrollStudentExecutor enrollStudent,
    EnrollmentWorkflowCheckpointStore checkpointStore,
    IEnrollmentWorkflowRecordStore recordStore,
    IEnrollmentService enrollmentService)
    {
        _workflow =
            EnrollmentWorkflowFactory.Create(
                validateStudent,
                validateCourse,
                checkExistingEnrollment,
                enrollmentRejected,
                prepareApproval,
                approvalRejected,
                enrollStudent);

        _checkpointStore = checkpointStore;
        _recordStore = recordStore;
        _enrollmentService = enrollmentService;
    }

    public async Task<EnrollmentWorkflowExecutionResult> RunAsync(
    EnrollmentWorkflowRequest request,
    CancellationToken cancellationToken = default)
    {
        RequestInfoEvent? pendingRequest = null;

        await using StreamingRun run =
            await InProcessExecution.RunStreamingAsync(
                _workflow,
                request,
                _checkpointStore.CheckpointManager,
                cancellationToken: cancellationToken);

        await foreach (
            WorkflowEvent workflowEvent
            in run.WatchStreamAsync())
        {
            switch (workflowEvent)
            {
                case ExecutorCompletedEvent executorCompleted:
                    Console.WriteLine(
                        $"Executor completed: {executorCompleted.ExecutorId}");
                    break;


                case RequestInfoEvent requestInfo:
                    pendingRequest = requestInfo;

                    Console.WriteLine(
                        $"Workflow requested external input. " +
                        $"RequestId: {requestInfo.Request.RequestId}");

                    break;


                case SuperStepCompletedEvent superStepCompleted:
                    {
                        CheckpointInfo? checkpoint =
                            superStepCompleted
                                .CompletionInfo?
                                .Checkpoint;

                        if (checkpoint is null)
                        {
                            break;
                        }

                        Console.WriteLine(
                            "Workflow checkpoint created.");

                        if (pendingRequest is not null)
                        {
                            await _recordStore.SavePendingAsync(
                                requestId: pendingRequest.Request.RequestId,
                                studentId: request.StudentId,
                                courseId: request.CourseId,
                                checkpointRunId: checkpoint.SessionId,
                                checkpointId: checkpoint.CheckpointId,
                                cancellationToken: cancellationToken);

                            return new EnrollmentWorkflowExecutionResult(
                                Status:
                                    EnrollmentWorkflowExecutionStatus
                                        .WaitingForApproval,

                                RequestId:
                                    pendingRequest.Request.RequestId,

                                StudentId:
                                    request.StudentId,

                                CourseId:
                                    request.CourseId,

                                Result:
                                    null,

                                Message:
                                    "Enrollment is waiting for human approval.");
                        }

                        break;
                    }


                case WorkflowOutputEvent output:
                    {
                        if (output.Data
                            is EnrollmentWorkflowResult result)
                        {
                            return new EnrollmentWorkflowExecutionResult(
                                Status:
                                    EnrollmentWorkflowExecutionStatus
                                        .Completed,

                                RequestId:
                                    null,

                                StudentId:
                                    result.StudentId,

                                CourseId:
                                    result.CourseId,

                                Result:
                                    result,

                                Message:
                                    result.Message);
                        }

                        break;
                    }


                case WorkflowErrorEvent error:
                    throw new InvalidOperationException(
                        "Enrollment workflow failed.",
                        error.Exception);
            }
        }

        throw new InvalidOperationException(
            "Enrollment workflow ended without producing a result or approval request.");
    }

    public async Task<EnrollmentWorkflowExecutionResult> ResumeAsync(
    string requestId,
    bool approved,
    CancellationToken cancellationToken = default)
    {
        var pendingWorkflow =
        await _recordStore.GetByRequestIdAsync(
            requestId,
            cancellationToken);

        if (pendingWorkflow is null)
        {
            throw new KeyNotFoundException(
                $"No pending enrollment workflow was found for request '{requestId}'.");
        }

        if (pendingWorkflow.Status != EnrollmentWorkflowStatus.WaitingForApproval)
        {
            throw new InvalidOperationException(
                $"Enrollment workflow '{requestId}' is not waiting for approval.");
        }

        bool processingStarted =
        await _recordStore.TryBeginProcessingAsync(
            requestId,
            approved,
            cancellationToken);

        if (!processingStarted)
        {
            throw new InvalidOperationException(
                $"Enrollment workflow '{requestId}' is already being processed or has already been completed.");
        }

        try
        {
            var checkpoint =
            new CheckpointInfo(
                pendingWorkflow.CheckpointRunId,
                pendingWorkflow.CheckpointId);

            await using StreamingRun run =
                await InProcessExecution.ResumeStreamingAsync(
                    _workflow,
                    checkpoint,
                    _checkpointStore.CheckpointManager,
                    cancellationToken);

            bool responseSent = false;

            await foreach (
                WorkflowEvent workflowEvent
                in run.WatchStreamAsync())
            {
                switch (workflowEvent)
                {
                    case RequestInfoEvent requestInfo
                        when requestInfo.Request.RequestId == requestId
                             && !responseSent:
                        {
                            Console.WriteLine(
                                $"Restored approval request: {requestId}");

                            var approvalResponse =
                            new EnrollmentApprovalResponse(
                                StudentId: pendingWorkflow.StudentId,
                                CourseId: pendingWorkflow.CourseId,
                                Approved: approved,
                                Reason: approved
                                    ? null
                                    : "Enrollment rejected by administrator.");

                            await run.SendResponseAsync(
                                requestInfo.Request.CreateResponse(
                                    approvalResponse));

                            responseSent = true;

                            Console.WriteLine(
                                $"Approval response sent. Approved: {approved}");

                            break;
                        }

                    case ExecutorCompletedEvent executorCompleted:
                        {
                            Console.WriteLine(
                                $"Executor completed: {executorCompleted.ExecutorId}");

                            break;
                        }

                    case SuperStepCompletedEvent:
                        {
                            Console.WriteLine(
                                "Resumed workflow super step completed.");

                            break;
                        }

                    case WorkflowOutputEvent output:
                        {
                            if (output.Data
                                is EnrollmentWorkflowResult result)
                            {
                                await _recordStore.MarkCompletedAsync(
                                    requestId,
                                    result.Success
                                        ? EnrollmentWorkflowStatus.Completed
                                        : EnrollmentWorkflowStatus.Rejected,
                                    cancellationToken);

                                return new EnrollmentWorkflowExecutionResult(
                                    Status:
                                        EnrollmentWorkflowExecutionStatus.Completed,

                                    RequestId:
                                        null,

                                    StudentId:
                                        result.StudentId,

                                    CourseId:
                                        result.CourseId,

                                    Result:
                                        result,

                                    Message:
                                        result.Message);
                            }

                            break;
                        }

                    case WorkflowErrorEvent error:
                        {
                            throw new InvalidOperationException(
                                "Enrollment workflow failed while resuming.",
                                error.Exception);
                        }
                }
            }

            throw new InvalidOperationException(
                "The enrollment workflow resumed but did not produce a final result.");
        }
        catch (Exception ex)
        {
            if (IsCancellationException(ex))
            {
                await _recordStore.MarkInterruptedAsync(
                    requestId,
                    CancellationToken.None);
            }
            else
            {
                await _recordStore.MarkFailedAsync(
                    requestId,
                    CancellationToken.None);
            }

            throw;
        }
    }

    public async Task<EnrollmentWorkflowRecoveryResult> RecoverAsync(
    string requestId,
    CancellationToken cancellationToken = default)
    {
        var workflowRecord =
            await _recordStore.GetByRequestIdAsync(
                requestId,
                cancellationToken);

        if (workflowRecord is null)
        {
            throw new KeyNotFoundException(
                $"No enrollment workflow was found for request '{requestId}'.");
        }

        if (workflowRecord.Status != EnrollmentWorkflowStatus.Failed &&
            workflowRecord.Status != EnrollmentWorkflowStatus.Interrupted)
        {
            throw new InvalidOperationException(
                $"Enrollment workflow '{requestId}' is not in a recoverable state.");
        }

        if (workflowRecord.Approved is false)
        {
            await _recordStore.MarkCompletedAsync(
                requestId,
                EnrollmentWorkflowStatus.Rejected,
                cancellationToken);

            return new EnrollmentWorkflowRecoveryResult(
                EnrollmentWorkflowRecoveryStatus.RecoveredAsRejected,
                requestId,
                workflowRecord.StudentId,
                workflowRecord.CourseId,
                "The original decision was rejection. The workflow has been finalized as rejected.");
        }

        if (workflowRecord.Approved is not true)
        {
            return new EnrollmentWorkflowRecoveryResult(
                EnrollmentWorkflowRecoveryStatus.ManualReviewRequired,
                requestId,
                workflowRecord.StudentId,
                workflowRecord.CourseId,
                "The workflow does not contain a valid approval decision and requires manual review.");
        }

        var existingEnrollment =
            _enrollmentService
                .GetEnrollmentsByStudent(workflowRecord.StudentId)
                .FirstOrDefault(x =>
                    x.CourseId == workflowRecord.CourseId &&
                    x.Status == "Active");

        if (existingEnrollment is not null)
        {
            await _recordStore.MarkCompletedAsync(
                requestId,
                EnrollmentWorkflowStatus.Completed,
                cancellationToken);

            return new EnrollmentWorkflowRecoveryResult(
                EnrollmentWorkflowRecoveryStatus.RecoveredAsCompleted,
                requestId,
                workflowRecord.StudentId,
                workflowRecord.CourseId,
                "The student is already actively enrolled. The workflow has been reconciled as completed.");
        }

        bool markedReady =
        await _recordStore.MarkReadyForRetryAsync(
            requestId,
            cancellationToken);

        if (!markedReady)
        {
            throw new InvalidOperationException(
                $"Enrollment workflow '{requestId}' could not be marked as ready for retry.");
        }

        return new EnrollmentWorkflowRecoveryResult(
            EnrollmentWorkflowRecoveryStatus.ReadyForRetry,
            requestId,
            workflowRecord.StudentId,
            workflowRecord.CourseId,
            "No active enrollment exists. The workflow can be retried safely.");
    }

    public async Task<EnrollmentWorkflowExecutionResult> RetryAsync(
    string requestId,
    CancellationToken cancellationToken = default)
    {
        var workflowRecord =
            await _recordStore.GetByRequestIdAsync(
                requestId,
                cancellationToken);

        if (workflowRecord is null)
        {
            throw new KeyNotFoundException(
                $"No enrollment workflow was found for request '{requestId}'.");
        }

        if (workflowRecord.Status !=
            EnrollmentWorkflowStatus.ReadyForRetry)
        {
            throw new InvalidOperationException(
                $"Enrollment workflow '{requestId}' is not ready for retry.");
        }

        if (workflowRecord.Approved is not true)
        {
            throw new InvalidOperationException(
                $"Enrollment workflow '{requestId}' does not contain an approved decision.");
        }

        var existingEnrollment =
            _enrollmentService
                .GetEnrollmentsByStudent(
                    workflowRecord.StudentId)
                .FirstOrDefault(x =>
                    x.CourseId == workflowRecord.CourseId &&
                    x.Status == "Active");

        if (existingEnrollment is not null)
        {
            await _recordStore.MarkCompletedAsync(
                requestId,
                EnrollmentWorkflowStatus.Completed,
                cancellationToken);

            return new EnrollmentWorkflowExecutionResult(
                EnrollmentWorkflowExecutionStatus.Completed,
                null,
                workflowRecord.StudentId,
                workflowRecord.CourseId,
                new EnrollmentWorkflowResult(
                    true,
                    workflowRecord.StudentId,
                    workflowRecord.CourseId,
                    "The student is already actively enrolled. The workflow was reconciled as completed."),
                "The student is already actively enrolled. The workflow was reconciled as completed.");
        }

        _enrollmentService.EnrollStudent(
            workflowRecord.StudentId,
            workflowRecord.CourseId);

        await _recordStore.MarkCompletedAsync(
            requestId,
            EnrollmentWorkflowStatus.Completed,
            cancellationToken);

        return new EnrollmentWorkflowExecutionResult(
            EnrollmentWorkflowExecutionStatus.Completed,
            null,
            workflowRecord.StudentId,
            workflowRecord.CourseId,
            new EnrollmentWorkflowResult(
                true,
                workflowRecord.StudentId,
                workflowRecord.CourseId,
                "Student enrolled successfully during retry."),
            "Student enrolled successfully during retry.");
    }

    private static bool IsCancellationException(
    Exception? exception)
    {
        while (exception is not null)
        {
            if (exception is OperationCanceledException)
            {
                return true;
            }

            exception = exception.InnerException;
        }

        return false;
    }
}
