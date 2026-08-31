using StudentManagement.Core.Enums;

namespace StudentManagement.Core.Models;

public sealed class CopilotTurnVersionRecord
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string ThreadId { get; set; } = string.Empty;

    public string UserMessageId { get; set; } = string.Empty;

    public int VersionNumber { get; set; }

    public string UserContent { get; set; } = string.Empty;

    public string? AssistantMessageId { get; set; }

    public string AssistantContent { get; set; } = string.Empty;

    public CopilotTurnStatus Status { get; set; }

    public string ActivitiesJson { get; set; } = "[]";

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}