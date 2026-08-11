using System.Text.Json;

namespace StudentManagement.AI.Sessions;

public record PendingToolApproval(
    string RequestId,
    string CallId,
    string FunctionName,
    IReadOnlyDictionary<string, object?> Arguments);

public interface ISessionStore
{
    Task<JsonElement?> GetAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        string sessionId,
        JsonElement serializedSession,
        CancellationToken cancellationToken = default);

    Task SavePendingApprovalAsync(
        string sessionId,
        PendingToolApproval approval,
        CancellationToken cancellationToken = default);

    Task<PendingToolApproval?> GetPendingApprovalAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    Task ClearPendingApprovalAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
}