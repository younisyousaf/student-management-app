namespace StudentManagement.AI.Workflows.Enrollment.Models;

public sealed record EnrollmentWorkflowSummary(
    string RequestId,
    int StudentId,
    int CourseId,
    string Status,
    bool? Approved,
    long? TotalDurationMs,
    long? ApprovalWaitMs,
    int CompletedExecutorCount,
    int FailureCount,
    int InterruptionCount,
    DateTime CreatedAt,
    DateTime? CompletedAt);