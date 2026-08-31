using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Enums;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;
using StudentManagement.Infrastructure.Hybrid;

namespace StudentManagementApp.WebApi.Services;

public sealed class CopilotTurnStore
{
    private readonly HybridDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public CopilotTurnStore(
        HybridDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<CopilotTurnRecord> EnsurePreparedAsync(
        string threadId,
        string userMessageId,
        CancellationToken cancellationToken = default)
    {
        int userId = GetRequiredUserId();

        var turn = await _dbContext.CopilotTurns.SingleOrDefaultAsync(
            x =>
                x.UserId == userId &&
                x.ThreadId == threadId &&
                x.UserMessageId == userMessageId,
            cancellationToken);

        if (turn is not null)
        {
            return turn;
        }

        DateTime now = DateTime.UtcNow;

        turn = new CopilotTurnRecord
        {
            UserId = userId,
            ThreadId = threadId,
            UserMessageId = userMessageId,
            Status = CopilotTurnStatus.Prepared,
            CurrentVersionNumber = 1,
            ActivitiesJson = "[]",
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.CopilotTurns.Add(turn);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return turn;
    }

    public async Task<CopilotTurnRecord?> MarkStoppedAsync(
        string threadId,
        string userMessageId,
        string activitiesJson,
        CancellationToken cancellationToken = default)
    {
        int userId = GetRequiredUserId();

        var turn = await _dbContext.CopilotTurns.SingleOrDefaultAsync(
            x =>
                x.UserId == userId &&
                x.ThreadId == threadId &&
                x.UserMessageId == userMessageId,
            cancellationToken);

        if (turn is null)
        {
            return null;
        }

        turn.Status = CopilotTurnStatus.Stopped;
        turn.ActivitiesJson = activitiesJson;
        turn.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return turn;
    }

    public async Task<IReadOnlyList<CopilotTurnRecord>> GetByThreadAsync(
        string threadId,
        CancellationToken cancellationToken = default)
    {
        int userId = GetRequiredUserId();

        return await _dbContext.CopilotTurns
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId &&
                x.ThreadId == threadId)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<CopilotTurnRecord?> MarkCompletedAsync(
    string threadId,
    string userMessageId,
    string userContent,
    string assistantMessageId,
    string assistantContent,
    string activitiesJson,
    CancellationToken cancellationToken = default)
    {
        int userId = GetRequiredUserId();

        var turn = await _dbContext.CopilotTurns.SingleOrDefaultAsync(
            x =>
                x.UserId == userId &&
                x.ThreadId == threadId &&
                x.UserMessageId == userMessageId,
            cancellationToken);

        if (turn is null)
        {
            return null;
        }

        int versionNumber = turn.CurrentVersionNumber;
        DateTime now = DateTime.UtcNow;

        /*
         * Idempotency:
         * RUN_FINISHED or the HTTP request could theoretically be retried.
         * Never create Version 1 twice.
         */
        var existingVersion = await _dbContext.CopilotTurnVersions
            .SingleOrDefaultAsync(
                x =>
                    x.UserId == userId &&
                    x.ThreadId == threadId &&
                    x.UserMessageId == userMessageId &&
                    x.VersionNumber == versionNumber,
                cancellationToken);

        if (existingVersion is null)
        {
            var version = new CopilotTurnVersionRecord
            {
                UserId = userId,
                ThreadId = threadId,
                UserMessageId = userMessageId,
                VersionNumber = versionNumber,
                UserContent = userContent,
                AssistantMessageId = assistantMessageId,
                AssistantContent = assistantContent,
                Status = CopilotTurnStatus.Completed,
                ActivitiesJson = activitiesJson,
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.CopilotTurnVersions.Add(version);
        }

        turn.Status = CopilotTurnStatus.Completed;
        turn.ActivitiesJson = activitiesJson;
        turn.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return turn;
    }

    public async Task<CopilotTurnRecord?> MarkPreparedForRerunAsync(
    string threadId,
    string userMessageId,
    CancellationToken cancellationToken = default)
    {
        int userId = GetRequiredUserId();

        var latestTurn = await _dbContext.CopilotTurns
            .Where(x =>
                x.UserId == userId &&
                x.ThreadId == threadId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (
            latestTurn is null ||
            latestTurn.UserMessageId != userMessageId ||
            latestTurn.Status != CopilotTurnStatus.Stopped
        )
        {
            return null;
        }

        latestTurn.Status = CopilotTurnStatus.Prepared;
        latestTurn.ActivitiesJson = "[]";
        latestTurn.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return latestTurn;
    }

    public async Task<CopilotTurnRecord?>
    BeginNextVersionAsync(
        string threadId,
        string userMessageId,
        CancellationToken cancellationToken = default)
    {
        int userId = GetRequiredUserId();

        var turn =
            await _dbContext.CopilotTurns
                .SingleOrDefaultAsync(
                    x =>
                        x.UserId == userId &&
                        x.ThreadId == threadId &&
                        x.UserMessageId == userMessageId,
                    cancellationToken);

        if (
            turn is null ||
            turn.Status !=
                CopilotTurnStatus.Completed
        )
        {
            return null;
        }

        /*
         * Do not create Version N+1 unless
         * Version N was successfully persisted.
         */
        bool currentVersionExists =
            await _dbContext
                .CopilotTurnVersions
                .AnyAsync(
                    x =>
                        x.UserId == userId &&
                        x.ThreadId == threadId &&
                        x.UserMessageId ==
                            userMessageId &&
                        x.VersionNumber ==
                            turn.CurrentVersionNumber &&
                        x.Status ==
                            CopilotTurnStatus.Completed,
                    cancellationToken);

        if (!currentVersionExists)
        {
            return null;
        }

        turn.CurrentVersionNumber++;
        turn.Status =
            CopilotTurnStatus.Prepared;

        turn.ActivitiesJson = "[]";
        turn.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return turn;
    }

    public async Task<IReadOnlyList<CopilotTurnVersionRecord>>
    GetVersionsAsync(
        string threadId,
        string userMessageId,
        CancellationToken cancellationToken = default)
    {
        int userId = GetRequiredUserId();

        return await _dbContext.CopilotTurnVersions
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId &&
                x.ThreadId == threadId &&
                x.UserMessageId == userMessageId)
            .OrderBy(x => x.VersionNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteByThreadAsync(
    string threadId,
    CancellationToken cancellationToken = default)
    {
        int userId = GetRequiredUserId();

        await _dbContext.CopilotTurnVersions
            .Where(x =>
                x.UserId == userId &&
                x.ThreadId == threadId)
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.CopilotTurns
            .Where(x =>
                x.UserId == userId &&
                x.ThreadId == threadId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private int GetRequiredUserId()
    {
        return _currentUserContext.UserId
            ?? throw new UnauthorizedAccessException(
                "An authenticated user is required to access Copilot turns.");
    }
}