using System.Text.Json;

namespace StudentManagement.AI.Sessions;

public interface ISessionStore
{
    JsonElement? Get(string sessionId);
    void Save(string sessionId, JsonElement serializedSession);
}