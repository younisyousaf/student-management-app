using Microsoft.EntityFrameworkCore;
using StudentManagement.AI.Sessions;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;
using StudentManagement.Infrastructure.Hybrid;
using System.Text.Json;

namespace StudentManagementApp.WebApi.Sessions;

public sealed class SqlServerSessionStore : ISessionStore
{
    private readonly HybridDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public SqlServerSessionStore(
        HybridDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<JsonElement?> GetAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        int userId = _currentUserContext.UserId
             ?? throw new UnauthorizedAccessException(
        "An authenticated user is required to access Copilot sessions.");

        AgentSessionRecord? entity =
            await _dbContext.AgentSessions
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.SessionId == sessionId &&
                         x.UserId == userId,
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
        int userId = _currentUserContext.UserId
             ?? throw new UnauthorizedAccessException(
        "An authenticated user is required to access Copilot sessions.");

        AgentSessionRecord? entity =
            await _dbContext.AgentSessions
                .SingleOrDefaultAsync(
                    x => x.SessionId == sessionId &&
                         x.UserId == userId,
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
                UserId = userId,
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