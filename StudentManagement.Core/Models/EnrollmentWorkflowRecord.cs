using StudentManagement.Core.Enums;

namespace StudentManagement.Core.Models;
public sealed class EnrollmentWorkflowRecord : BaseEntity
{
    public string RequestId { get; set; } = string.Empty;

    public int StudentId { get; set; }

    public int CourseId { get; set; }

    public EnrollmentWorkflowStatus Status { get; set; }

    public bool? Approved { get; set; }

    public string? ActiveKey { get; set; }

    public string CheckpointRunId { get; set; } = string.Empty;

    public string CheckpointId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
