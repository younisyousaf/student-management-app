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

        return await SessionStoreExecution.ExecuteAsync(
            async () =>
            {
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

                return (JsonElement?)document.RootElement.Clone();
            });
    }

    public async Task SaveAsync(
    string sessionId,
    JsonElement serializedSession,
    CancellationToken cancellationToken = default)
    {
        int userId = _currentUserContext.UserId
            ?? throw new UnauthorizedAccessException(
                "An authenticated user is required to access Copilot sessions.");

        await SessionStoreExecution.ExecuteAsync(
            async () =>
            {
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

                await _dbContext.SaveChangesAsync(
                    cancellationToken);
            });
    }

    public async Task SavePendingApprovalAsync(
    string sessionId,
    PendingToolApproval approval,
    CancellationToken cancellationToken = default)
    {
        int userId = _currentUserContext.UserId
            ?? throw new UnauthorizedAccessException(
                "An authenticated user is required to access Copilot sessions.");

        await SessionStoreExecution.ExecuteAsync(
            async () =>
            {
                var entity = await _dbContext.AgentSessions
                    .SingleAsync(
                        x => x.SessionId == sessionId &&
                             x.UserId == userId,
                        cancellationToken);

                entity.PendingApprovalRequestId = approval.RequestId;
                entity.PendingApprovalCallId = approval.CallId;
                entity.PendingApprovalFunctionName = approval.FunctionName;
                entity.PendingApprovalArgumentsJson =
                    JsonSerializer.Serialize(approval.Arguments);

                entity.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(cancellationToken);
            });
    }

    public async Task<PendingToolApproval?> GetPendingApprovalAsync(
    string sessionId,
    CancellationToken cancellationToken = default)
    {
        int userId = _currentUserContext.UserId
            ?? throw new UnauthorizedAccessException(
                "An authenticated user is required to access Copilot sessions.");

        return await SessionStoreExecution.ExecuteAsync(
            async () =>
            {
                var entity = await _dbContext.AgentSessions
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        x => x.SessionId == sessionId &&
                             x.UserId == userId,
                        cancellationToken);

                if (entity is null ||
                    string.IsNullOrWhiteSpace(entity.PendingApprovalRequestId) ||
                    string.IsNullOrWhiteSpace(entity.PendingApprovalCallId) ||
                    string.IsNullOrWhiteSpace(entity.PendingApprovalFunctionName))
                {
                    return null;
                }

                var arguments =
                    string.IsNullOrWhiteSpace(entity.PendingApprovalArgumentsJson)
                        ? new Dictionary<string, object?>()
                        : JsonSerializer.Deserialize<Dictionary<string, object?>>(
                            entity.PendingApprovalArgumentsJson)
                          ?? new Dictionary<string, object?>();

                return new PendingToolApproval(
                    entity.PendingApprovalRequestId,
                    entity.PendingApprovalCallId,
                    entity.PendingApprovalFunctionName,
                    arguments);
            });
    }

    public async Task ClearPendingApprovalAsync(
    string sessionId,
    CancellationToken cancellationToken = default)
    {
        int userId = _currentUserContext.UserId
            ?? throw new UnauthorizedAccessException(
                "An authenticated user is required to access Copilot sessions.");

        await SessionStoreExecution.ExecuteAsync(
            async () =>
            {
                var entity = await _dbContext.AgentSessions
                    .SingleOrDefaultAsync(
                        x => x.SessionId == sessionId &&
                             x.UserId == userId,
                        cancellationToken);

                if (entity is null)
                {
                    return;
                }

                entity.PendingApprovalRequestId = null;
                entity.PendingApprovalCallId = null;
                entity.PendingApprovalFunctionName = null;
                entity.PendingApprovalArgumentsJson = null;
                entity.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(cancellationToken);
            });
    }
}