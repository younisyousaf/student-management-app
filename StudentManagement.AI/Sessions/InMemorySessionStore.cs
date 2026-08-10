using System.Collections.Concurrent;
using System.Text.Json;

namespace StudentManagement.AI.Sessions;

// TEMPORARY: in-process only. Sessions are lost on restart and won't work
// across multiple server instances. Replaced by a SQL Server-backed
// implementation in Phase 8 — CopilotService only depends on ISessionStore,
// so that swap won't require any change here.
public class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<string, JsonElement> _sessions = new();

    public JsonElement? Get(string sessionId) =>
        _sessions.TryGetValue(sessionId, out var session) ? session : null;

    public void Save(string sessionId, JsonElement serializedSession) =>
        _sessions[sessionId] = serializedSession;
}