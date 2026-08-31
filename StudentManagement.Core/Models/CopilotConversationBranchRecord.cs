namespace StudentManagement.Core.Models;

public sealed class CopilotConversationBranchRecord
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ThreadId { get; set; } = string.Empty;
    public string BranchId { get; set; } = string.Empty;
    public string? ParentBranchId { get; set; }
    public string? BranchedFromUserMessageId { get; set; }
    public int? BranchedFromVersionNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}