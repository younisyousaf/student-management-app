using System.Collections.Concurrent;
using System.Text.Json;

namespace StudentManagement.AI.Sessions;

public class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<string, JsonElement> _sessions = new();

    private readonly ConcurrentDictionary<string, PendingToolApproval>
        _pendingApprovals = new();

    public Task<JsonElement?> GetAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        JsonElement? result =
            _sessions.TryGetValue(sessionId, out var session)
                ? session
                : null;

        return Task.FromResult(result);
    }

    public Task SaveAsync(
        string sessionId,
        JsonElement serializedSession,
        CancellationToken cancellationToken = default)
    {
        _sessions[sessionId] = serializedSession;

        return Task.CompletedTask;
    }

    public Task SavePendingApprovalAsync(
        string sessionId,
        PendingToolApproval approval,
        CancellationToken cancellationToken = default)
    {
        _pendingApprovals[sessionId] = approval;

        return Task.CompletedTask;
    }

    public Task<PendingToolApproval?> GetPendingApprovalAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        PendingToolApproval? result =
            _pendingApprovals.TryGetValue(
                sessionId,
                out var approval)
                ? approval
                : null;

        return Task.FromResult(result);
    }

    public Task ClearPendingApprovalAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        _pendingApprovals.TryRemove(
            sessionId,
            out _);

        return Task.CompletedTask;
    }

    public Task DeleteAsync(
    string sessionId,
    CancellationToken cancellationToken = default)
    {
        _sessions.TryRemove(
            sessionId,
            out _);

        _pendingApprovals.TryRemove(
            sessionId,
            out _);

        return Task.CompletedTask;
    }
}