using Microsoft.EntityFrameworkCore;
using StudentManagement.AI.Sessions;
using StudentManagement.Core.Models;
using StudentManagement.Infrastructure.Hybrid;
using System.Text.Json;

namespace StudentManagementApp.WebApi.Sessions;

public sealed class SqlServerSessionStore : ISessionStore
{
    private readonly HybridDbContext _dbContext;

    public SqlServerSessionStore(HybridDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<JsonElement?> GetAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        AgentSessionRecord? entity =
            await _dbContext.AgentSessions
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.SessionId == sessionId,
                    cancellationToken);

        if (entity is null)
        {
            return null;
        }

        using JsonDocument document =
            JsonDocument.Parse(entity.SerializedSession);

        return document.RootElement.Clone();
    }

    public async Task SaveAsync(
        string sessionId,
        JsonElement serializedSession,
        CancellationToken cancellationToken = default)
    {
        AgentSessionRecord? entity =
            await _dbContext.AgentSessions
                .SingleOrDefaultAsync(
                    x => x.SessionId == sessionId,
                    cancellationToken);

        string json = serializedSession.GetRawText();

        if (entity is null)
        {
            entity = new AgentSessionRecord
            {
                SessionId = sessionId,
                SerializedSession = json,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UserId = null,
                ExpiresAt = null
            };

            _dbContext.AgentSessions.Add(entity);
        }
        else
        {
            entity.SerializedSession = json;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}