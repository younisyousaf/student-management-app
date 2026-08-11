using Microsoft.Agents.AI;
using StudentManagement.AI.Sessions;
using System.Text.Json;

namespace StudentManagement.AI.Services;

public class CopilotService : ICopilotService
{
    private readonly AIAgent _agent;
    private readonly ISessionStore _sessionStore;

    public CopilotService(
        AIAgent agent,
        ISessionStore sessionStore)
    {
        _agent = agent;
        _sessionStore = sessionStore;
    }

    public async Task<CopilotChatResult> SendMessageAsync(
        string message,
        string? sessionId,
        CancellationToken cancellationToken = default)
    {
        AgentSession session;
        string resolvedSessionId;

        JsonElement? existingSession = null;

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            existingSession = await _sessionStore.GetAsync(
                sessionId,
                cancellationToken);
        }

        if (existingSession is { } existing)
        {
            session = await _agent.DeserializeSessionAsync(
                existing,
                cancellationToken: cancellationToken);

            resolvedSessionId = sessionId!;
        }
        else
        {
            session = await _agent.CreateSessionAsync(
                cancellationToken);

            resolvedSessionId = Guid.NewGuid().ToString();
        }

        AgentResponse result = await _agent.RunAsync(
            message,
            session,
            cancellationToken: cancellationToken);

        JsonElement serialized =
            await _agent.SerializeSessionAsync(
                session,
                cancellationToken: cancellationToken);

        await _sessionStore.SaveAsync(
            resolvedSessionId,
            serialized,
            cancellationToken);

        return new CopilotChatResult(
            result.Text,
            resolvedSessionId);
    }
}