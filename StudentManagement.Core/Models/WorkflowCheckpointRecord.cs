namespace StudentManagement.Core.Models;

public sealed class WorkflowCheckpointRecord : BaseEntity
{
    public string SessionId { get; set; } = string.Empty;

    public string CheckpointId { get; set; } = string.Empty;

    public string? ParentCheckpointId { get; set; }

    public string CheckpointData { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
