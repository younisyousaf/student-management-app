namespace StudentManagement.AI.Workflows.Enrollment.Models;

public sealed record EnrollmentWorkflowRequest(
    int StudentId,
    int CourseId);

public sealed record EnrollmentWorkflowResult(
    bool Success,
    int StudentId,
    int CourseId,
    string Message);

public sealed record EnrollmentValidationResult(
    int StudentId,
    int CourseId,
    bool CanEnroll,
    string Message);

public sealed record EnrollmentApprovalRequest(
    int StudentId,
    int CourseId,
    string Message);

public sealed record EnrollmentApprovalResponse(
    int StudentId,
    int CourseId,
    bool Approved,
    string? Reason = null);

public enum EnrollmentWorkflowExecutionStatus
{
    Completed,
    WaitingForApproval
}

public sealed record EnrollmentWorkflowExecutionResult(
    EnrollmentWorkflowExecutionStatus Status,
    string? RequestId,
    int StudentId,
    int CourseId,
    EnrollmentWorkflowResult? Result,
    string Message);

public sealed record EnrollmentWorkflowApprovalDecision(
    string RequestId,
    bool Approved);
