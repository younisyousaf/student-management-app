namespace StudentManagement.Core.Models;

public sealed class CopilotConversationRecord
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string ThreadId { get; set; } =
        string.Empty;

    public string Title { get; set; } =
        string.Empty;

    public string? LastRunId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}