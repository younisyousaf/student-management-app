namespace StudentManagement.Core.Models;

public sealed class CopilotBranchTurnRecord
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ThreadId { get; set; } = string.Empty;
    public string BranchId { get; set; } = string.Empty;
    public string UserMessageId { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public int Position { get; set; }
    public DateTime CreatedAt { get; set; }
}