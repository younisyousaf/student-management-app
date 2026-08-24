using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.DependencyInjection;
using StudentManagement.AI.Sessions;

namespace StudentManagementApp.WebApi.Sessions;

public sealed class SqlHostedAgentSessionStore
    : AgentSessionStore
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SqlHostedAgentSessionStore(
        IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public override async ValueTask<AgentSession> GetSessionAsync(
        AIAgent agent,
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        await using var scope =
            _scopeFactory.CreateAsyncScope();

        var sessionStore =
            scope.ServiceProvider
                .GetRequiredService<ISessionStore>();

        JsonElement? serialized =
            await sessionStore.GetAsync(
                conversationId,
                cancellationToken);

        if (serialized is { } existing)
        {
            return await agent.DeserializeSessionAsync(
                existing,
                cancellationToken: cancellationToken);
        }

        return await agent.CreateSessionAsync(
            cancellationToken);
    }

    public override async ValueTask SaveSessionAsync(
        AIAgent agent,
        string conversationId,
        AgentSession session,
        CancellationToken cancellationToken = default)
    {
        await using var scope =
            _scopeFactory.CreateAsyncScope();

        var sessionStore =
            scope.ServiceProvider
                .GetRequiredService<ISessionStore>();

        JsonElement serialized =
            await agent.SerializeSessionAsync(
                session,
                cancellationToken: cancellationToken);

        await sessionStore.SaveAsync(
            conversationId,
            serialized,
            cancellationToken);
    }

    public override async ValueTask DeleteSessionAsync(
        AIAgent agent,
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        await using var scope =
            _scopeFactory.CreateAsyncScope();

        var sessionStore =
            scope.ServiceProvider
                .GetRequiredService<ISessionStore>();

        await sessionStore.DeleteAsync(
            conversationId,
            cancellationToken);
    }
}
