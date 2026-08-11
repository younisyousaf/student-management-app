using System.Collections.Concurrent;
using System.Text.Json;

namespace StudentManagement.AI.Sessions;

public class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<string, JsonElement> _sessions = new();

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
}