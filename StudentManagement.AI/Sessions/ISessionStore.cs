using System.Text.Json;

namespace StudentManagement.AI.Sessions;

public interface ISessionStore
{
    Task<JsonElement?> GetAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        string sessionId,
        JsonElement serializedSession,
        CancellationToken cancellationToken = default);
}