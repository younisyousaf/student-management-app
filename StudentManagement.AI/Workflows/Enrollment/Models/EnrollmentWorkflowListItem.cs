namespace StudentManagement.AI.Workflows.Enrollment.Models;

public sealed record EnrollmentWorkflowListItem(
    string RequestId,
    int StudentId,
    int CourseId,
    string Status,
    bool? Approved,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? CompletedAt);