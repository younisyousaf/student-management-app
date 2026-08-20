using System.Diagnostics;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<EnrollmentWorkflowService> _logger;

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
        IEnrollmentService enrollmentService,
        ILogger<EnrollmentWorkflowService> logger)
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
        _logger = logger;
    }

    public async Task<EnrollmentWorkflowExecutionResult> RunAsync(
        EnrollmentWorkflowRequest request,
        CancellationToken cancellationToken = default)
    {
        using var logScope =
            _logger.BeginScope(
                new Dictionary<string, object?>
                {
                    ["WorkflowName"] = "Enrollment",
                    ["StudentId"] = request.StudentId,
                    ["CourseId"] = request.CourseId
                });

        _logger.LogInformation(
            "Enrollment workflow started.");

        RequestInfoEvent? pendingRequest = null;

        var executorTimers =
            new Dictionary<string, Stopwatch>();

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
                case ExecutorInvokedEvent executorInvoked:
                    {
                        var stopwatch =
                            Stopwatch.StartNew();

                        executorTimers[
                            executorInvoked.ExecutorId] =
                            stopwatch;

                        _logger.LogDebug(
                            "Enrollment workflow executor started. ExecutorId: {ExecutorId}",
                            executorInvoked.ExecutorId);

                        break;
                    }

                case ExecutorCompletedEvent executorCompleted:
                    {
                        if (executorTimers.Remove(
                            executorCompleted.ExecutorId,
                            out var stopwatch))
                        {
                            stopwatch.Stop();

                            _logger.LogInformation(
                                "Enrollment workflow executor completed. ExecutorId: {ExecutorId}, DurationMs: {DurationMs}",
                                executorCompleted.ExecutorId,
                                stopwatch.ElapsedMilliseconds);
                        }
                        else
                        {
                            _logger.LogInformation(
                                "Enrollment workflow executor completed. ExecutorId: {ExecutorId}",
                                executorCompleted.ExecutorId);
                        }

                        break;
                    }

                case ExecutorFailedEvent executorFailed:
                    {
                        if (executorTimers.Remove(
                            executorFailed.ExecutorId,
                            out var stopwatch))
                        {
                            stopwatch.Stop();

                            _logger.LogError(
                                "Enrollment workflow executor failed. ExecutorId: {ExecutorId}, DurationMs: {DurationMs}",
                                executorFailed.ExecutorId,
                                stopwatch.ElapsedMilliseconds);
                        }
                        else
                        {
                            _logger.LogError(
                                "Enrollment workflow executor failed. ExecutorId: {ExecutorId}",
                                executorFailed.ExecutorId);
                        }

                        break;
                    }

                case RequestInfoEvent requestInfo:
                    {
                        pendingRequest = requestInfo;

                        _logger.LogInformation(
                            "Enrollment workflow is waiting for human approval. RequestId: {RequestId}",
                            requestInfo.Request.RequestId);

                        break;
                    }

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

                        _logger.LogDebug(
                            "Enrollment workflow checkpoint created.");

                        if (pendingRequest is not null)
                        {
                            await _recordStore.SavePendingAsync(
                                requestId:
                                    pendingRequest.Request.RequestId,
                                studentId:
                                    request.StudentId,
                                courseId:
                                    request.CourseId,
                                checkpointRunId:
                                    checkpoint.SessionId,
                                checkpointId:
                                    checkpoint.CheckpointId,
                                cancellationToken:
                                    cancellationToken);

                            _logger.LogInformation(
                                "Enrollment workflow paused and persisted while waiting for human approval. RequestId: {RequestId}",
                                pendingRequest.Request.RequestId);

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
                            _logger.LogInformation(
                                "Enrollment workflow completed. Success: {Success}",
                                result.Success);

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
                    {
                        _logger.LogError(
                            error.Exception,
                            "Enrollment workflow execution failed.");

                        throw new InvalidOperationException(
                            "Enrollment workflow failed.",
                            error.Exception);
                    }
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

        using var logScope =
            _logger.BeginScope(
                new Dictionary<string, object?>
                {
                    ["WorkflowName"] = "Enrollment",
                    ["RequestId"] = requestId,
                    ["StudentId"] = pendingWorkflow.StudentId,
                    ["CourseId"] = pendingWorkflow.CourseId
                });

        var approvalWaitDuration =
            DateTime.UtcNow - pendingWorkflow.UpdatedAt;

        _logger.LogInformation(
            "Enrollment workflow approval received. ApprovalWaitMs: {ApprovalWaitMs}, ApprovalWaitSeconds: {ApprovalWaitSeconds}",
            approvalWaitDuration.TotalMilliseconds,
            approvalWaitDuration.TotalSeconds);

        _logger.LogInformation(
            "Enrollment workflow resume started. Approved: {Approved}",
            approved);

        if (pendingWorkflow.Status !=
            EnrollmentWorkflowStatus.WaitingForApproval)
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

        _logger.LogInformation(
            "Enrollment workflow claimed for processing.");

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

            var executorTimers =
                new Dictionary<string, Stopwatch>();

            await foreach (
                WorkflowEvent workflowEvent
                in run.WatchStreamAsync())
            {
                switch (workflowEvent)
                {
                    case ExecutorInvokedEvent executorInvoked:
                        {
                            var stopwatch =
                                Stopwatch.StartNew();

                            executorTimers[
                                executorInvoked.ExecutorId] =
                                stopwatch;

                            _logger.LogDebug(
                                "Enrollment workflow executor started after resume. ExecutorId: {ExecutorId}",
                                executorInvoked.ExecutorId);

                            break;
                        }

                    case RequestInfoEvent requestInfo
                        when requestInfo.Request.RequestId == requestId
                             && !responseSent:
                        {
                            _logger.LogInformation(
                                "Enrollment workflow approval request restored.");

                            var approvalResponse =
                                new EnrollmentApprovalResponse(
                                    StudentId:
                                        pendingWorkflow.StudentId,
                                    CourseId:
                                        pendingWorkflow.CourseId,
                                    Approved:
                                        approved,
                                    Reason:
                                        approved
                                            ? null
                                            : "Enrollment rejected by administrator.");

                            await run.SendResponseAsync(
                                requestInfo.Request.CreateResponse(
                                    approvalResponse));

                            responseSent = true;

                            _logger.LogInformation(
                                "Enrollment workflow approval decision sent. Approved: {Approved}",
                                approved);

                            break;
                        }

                    case ExecutorCompletedEvent executorCompleted:
                        {
                            if (executorTimers.Remove(
                                executorCompleted.ExecutorId,
                                out var stopwatch))
                            {
                                stopwatch.Stop();

                                _logger.LogInformation(
                                    "Enrollment workflow executor completed after resume. ExecutorId: {ExecutorId}, DurationMs: {DurationMs}",
                                    executorCompleted.ExecutorId,
                                    stopwatch.ElapsedMilliseconds);
                            }
                            else
                            {
                                _logger.LogInformation(
                                    "Enrollment workflow executor completed after resume. ExecutorId: {ExecutorId}",
                                    executorCompleted.ExecutorId);
                            }

                            break;
                        }

                    case ExecutorFailedEvent executorFailed:
                        {
                            if (executorTimers.Remove(
                                executorFailed.ExecutorId,
                                out var stopwatch))
                            {
                                stopwatch.Stop();

                                _logger.LogError(
                                    "Enrollment workflow executor failed after resume. ExecutorId: {ExecutorId}, DurationMs: {DurationMs}",
                                    executorFailed.ExecutorId,
                                    stopwatch.ElapsedMilliseconds);
                            }
                            else
                            {
                                _logger.LogError(
                                    "Enrollment workflow executor failed after resume. ExecutorId: {ExecutorId}",
                                    executorFailed.ExecutorId);
                            }

                            break;
                        }

                    case SuperStepCompletedEvent:
                        {
                            _logger.LogDebug(
                                "Enrollment workflow resumed super step completed.");

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

                                var totalWorkflowDuration =
                                    DateTime.UtcNow - pendingWorkflow.CreatedAt;

                                _logger.LogInformation(
                                    "Enrollment workflow completed after resume. Success: {Success}, TotalDurationMs: {TotalDurationMs}, TotalDurationSeconds: {TotalDurationSeconds}",
                                    result.Success,
                                    totalWorkflowDuration.TotalMilliseconds,
                                    totalWorkflowDuration.TotalSeconds);

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

                _logger.LogWarning(
                    ex,
                    "Enrollment workflow interrupted.");
            }
            else
            {
                await _recordStore.MarkFailedAsync(
                    requestId,
                    CancellationToken.None);

                _logger.LogError(
                    ex,
                    "Enrollment workflow failed.");
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

        using var logScope =
            _logger.BeginScope(
                new Dictionary<string, object?>
                {
                    ["WorkflowName"] = "Enrollment",
                    ["RequestId"] = requestId,
                    ["StudentId"] = workflowRecord.StudentId,
                    ["CourseId"] = workflowRecord.CourseId,
                    ["WorkflowStatus"] = workflowRecord.Status
                });

        _logger.LogInformation(
            "Enrollment workflow recovery started.");

        if (workflowRecord.Status !=
                EnrollmentWorkflowStatus.Failed &&
            workflowRecord.Status !=
                EnrollmentWorkflowStatus.Interrupted)
        {
            _logger.LogWarning(
                "Enrollment workflow recovery rejected because the workflow is not in a recoverable state.");

            throw new InvalidOperationException(
                $"Enrollment workflow '{requestId}' is not in a recoverable state.");
        }

        if (workflowRecord.Approved is false)
        {
            await _recordStore.MarkCompletedAsync(
                requestId,
                EnrollmentWorkflowStatus.Rejected,
                cancellationToken);

            _logger.LogInformation(
                "Enrollment workflow recovered as rejected.");

            return new EnrollmentWorkflowRecoveryResult(
                EnrollmentWorkflowRecoveryStatus.RecoveredAsRejected,
                requestId,
                workflowRecord.StudentId,
                workflowRecord.CourseId,
                "The original decision was rejection. The workflow has been finalized as rejected.");
        }

        if (workflowRecord.Approved is not true)
        {
            _logger.LogWarning(
                "Enrollment workflow requires manual review because no valid approval decision is available.");

            return new EnrollmentWorkflowRecoveryResult(
                EnrollmentWorkflowRecoveryStatus.ManualReviewRequired,
                requestId,
                workflowRecord.StudentId,
                workflowRecord.CourseId,
                "The workflow does not contain a valid approval decision and requires manual review.");
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

            _logger.LogInformation(
                "Enrollment workflow reconciled as completed because an active enrollment already exists.");

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
            _logger.LogError(
                "Enrollment workflow could not be marked ready for retry.");

            throw new InvalidOperationException(
                $"Enrollment workflow '{requestId}' could not be marked as ready for retry.");
        }

        _logger.LogInformation(
            "Enrollment workflow marked ready for retry.");

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

        using var logScope =
            _logger.BeginScope(
                new Dictionary<string, object?>
                {
                    ["WorkflowName"] = "Enrollment",
                    ["RequestId"] = requestId,
                    ["StudentId"] = workflowRecord.StudentId,
                    ["CourseId"] = workflowRecord.CourseId,
                    ["WorkflowStatus"] = workflowRecord.Status
                });

        _logger.LogInformation(
            "Enrollment workflow retry started.");

        if (workflowRecord.Status !=
            EnrollmentWorkflowStatus.ReadyForRetry)
        {
            _logger.LogWarning(
                "Enrollment workflow retry rejected because the workflow is not ready for retry.");

            throw new InvalidOperationException(
                $"Enrollment workflow '{requestId}' is not ready for retry.");
        }

        if (workflowRecord.Approved is not true)
        {
            _logger.LogWarning(
                "Enrollment workflow retry rejected because an approved decision is not available.");

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

            _logger.LogInformation(
                "Retry skipped because the student is already actively enrolled. Workflow reconciled as completed.");

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

        _logger.LogInformation(
            "Executing enrollment business operation during retry.");

        _enrollmentService.EnrollStudent(
            workflowRecord.StudentId,
            workflowRecord.CourseId);

        await _recordStore.MarkCompletedAsync(
            requestId,
            EnrollmentWorkflowStatus.Completed,
            cancellationToken);

        _logger.LogInformation(
            "Enrollment workflow retry completed successfully.");

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