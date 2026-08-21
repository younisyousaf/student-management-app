namespace StudentManagement.Core.Models;

public sealed class EnrollmentWorkflowHistory
{
    public int Id { get; set; }

    public string RequestId { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public string? ExecutorId { get; set; }

    public long? DurationMs { get; set; }

    public string? Message { get; set; }

    public DateTime OccurredAt { get; set; }
}