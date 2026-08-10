using Microsoft.Agents.AI;
using StudentManagement.AI.Sessions;

namespace StudentManagement.AI.Services;

public class CopilotService : ICopilotService
{
    private readonly AIAgent _agent;
    private readonly ISessionStore _sessionStore;

    public CopilotService(AIAgent agent, ISessionStore sessionStore)
    {
        _agent = agent;
        _sessionStore = sessionStore;
    }

    public async Task<CopilotChatResult> SendMessageAsync(string message, string? sessionId, CancellationToken cancellationToken = default)
    {
        AgentSession session;
        string resolvedSessionId;

        if (!string.IsNullOrWhiteSpace(sessionId) && _sessionStore.Get(sessionId) is { } existing)
        {
            session = await _agent.DeserializeSessionAsync(existing, cancellationToken: cancellationToken);
            resolvedSessionId = sessionId;
        }
        else
        {
            session = await _agent.CreateSessionAsync(cancellationToken);
            resolvedSessionId = Guid.NewGuid().ToString();
        }

        var result = await _agent.RunAsync(message, session, cancellationToken: cancellationToken);

        var serialized = await _agent.SerializeSessionAsync(session, cancellationToken: cancellationToken);
        _sessionStore.Save(resolvedSessionId, serialized);

        return new CopilotChatResult(result.Text, resolvedSessionId);
    }
}