using StudentManagement.Core.Enums;

namespace StudentManagement.AI.Workflows.Enrollment.Models;

public sealed class EnrollmentWorkflowQuery
{
    public EnrollmentWorkflowStatus? Status { get; init; }

    public int? StudentId { get; init; }

    public int? CourseId { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}
