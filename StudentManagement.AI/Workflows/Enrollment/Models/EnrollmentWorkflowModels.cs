using System.ComponentModel.DataAnnotations;

namespace StudentManagement.AI.Workflows.Enrollment.Models;

public sealed record EnrollmentWorkflowRequest(
    [Range(1, int.MaxValue)]
    int StudentId,

    [Range(1, int.MaxValue)]
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
    [Required]
    [MinLength(1)]
    string RequestId,

    bool Approved);

public enum EnrollmentWorkflowRecoveryStatus
{
    RecoveredAsCompleted,
    RecoveredAsRejected,
    ReadyForRetry,
    ManualReviewRequired
}

public sealed record EnrollmentWorkflowRecoveryResult(
    EnrollmentWorkflowRecoveryStatus Status,
    string RequestId,
    int StudentId,
    int CourseId,
    string Message);
