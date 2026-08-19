using Microsoft.Agents.AI.Workflows;
using StudentManagement.AI.Workflows.Enrollment.Executors;
using StudentManagement.AI.Workflows.Enrollment.Models;
using StudentManagement.Core.Enums;

namespace StudentManagement.AI.Workflows.Enrollment;

public sealed class EnrollmentWorkflowService
{
    private readonly Workflow _workflow;
    private readonly EnrollmentWorkflowCheckpointStore _checkpointStore;
    private readonly IEnrollmentWorkflowRecordStore _recordStore;

    public EnrollmentWorkflowService(
        ValidateStudentExecutor validateStudent,
        ValidateCourseExecutor validateCourse,
        CheckExistingEnrollmentExecutor checkExistingEnrollment,
        EnrollmentRejectedExecutor enrollmentRejected,
        PrepareEnrollmentApprovalExecutor prepareApproval,
        EnrollmentApprovalRejectedExecutor approvalRejected,
        EnrollStudentExecutor enrollStudent,
        EnrollmentWorkflowCheckpointStore checkpointStore,
        IEnrollmentWorkflowRecordStore recordStore)
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
}
