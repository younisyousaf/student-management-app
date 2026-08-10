namespace StudentManagement.AI.Services;

public record CopilotChatResult(string Response, string SessionId);

public interface ICopilotService
{
    Task<CopilotChatResult> SendMessageAsync(string message, string? sessionId, CancellationToken cancellationToken = default);
}