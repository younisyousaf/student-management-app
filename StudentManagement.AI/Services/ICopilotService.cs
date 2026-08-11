using System.Collections.Generic;

namespace StudentManagement.AI.Services;

public record CopilotApprovalRequest(
    string RequestId,
    string FunctionName,
    IReadOnlyDictionary<string, object?> Arguments);

public record CopilotApprovalResult(
    string? Response,
    string SessionId,
    bool Approved);

public record CopilotChatResult(
    string? Response,
    string SessionId,
    bool RequiresApproval,
    CopilotApprovalRequest? Approval);

public interface ICopilotService
{
    Task<CopilotChatResult> SendMessageAsync(
       string message,
       string? sessionId,
       CancellationToken cancellationToken = default);

    Task<CopilotApprovalResult> RespondToApprovalAsync(
        string sessionId,
        string requestId,
        bool approved,
        string? reason = null,
        CancellationToken cancellationToken = default);
}