using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Enums;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;
using StudentManagement.Infrastructure.Hybrid;

namespace StudentManagementApp.WebApi.Services;

public sealed record CopilotBranchTurnSnapshot(
    string UserMessageId,
    int VersionNumber,
    int Position,
    string UserContent,
    string? AssistantMessageId,
    string AssistantContent,
    CopilotTurnStatus Status,
    string ActivitiesJson);

public sealed record CopilotBranchSnapshot(
    string BranchId,
    string? ParentBranchId,
    string? BranchedFromUserMessageId,
    int? BranchedFromVersionNumber,
    IReadOnlyList<CopilotBranchTurnSnapshot> Turns);

public sealed class CopilotBranchStore
{
    private readonly HybridDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public CopilotBranchStore(HybridDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<CopilotConversationBranchRecord?> EnsureActiveBranchAsync(
        string threadId,
        IReadOnlyList<string> activeUserMessageIds,
        CancellationToken cancellationToken = default)
    {
        int userId = GetRequiredUserId();

        var conversation = await _dbContext.CopilotConversations.SingleOrDefaultAsync(
            x => x.UserId == userId && x.ThreadId == threadId,
            cancellationToken);

        if (conversation is null)
            return null;

        CopilotConversationBranchRecord? branch = null;

        if (!string.IsNullOrWhiteSpace(conversation.ActiveBranchId))
        {
            branch = await _dbContext.CopilotConversationBranches.SingleOrDefaultAsync(
                x => x.UserId == userId &&
                     x.ThreadId == threadId &&
                     x.BranchId == conversation.ActiveBranchId,
                cancellationToken);
        }

        if (branch is null)
        {
            DateTime now = DateTime.UtcNow;

            branch = new CopilotConversationBranchRecord
            {
                UserId = userId,
                ThreadId = threadId,
                BranchId = Guid.NewGuid().ToString("N"),
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.CopilotConversationBranches.Add(branch);
            conversation.ActiveBranchId = branch.BranchId;
            conversation.UpdatedAt = now;
        }

        await SyncBranchTurnsAsync(branch.BranchId, threadId, activeUserMessageIds, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return branch;
    }

    public async Task<CopilotConversationBranchRecord?> CreateBranchFromEditAsync(
        string threadId,
        string userMessageId,
        int newVersionNumber,
        CancellationToken cancellationToken = default)
    {
        int userId = GetRequiredUserId();

        var conversation = await _dbContext.CopilotConversations.SingleOrDefaultAsync(
            x => x.UserId == userId && x.ThreadId == threadId,
            cancellationToken);

        if (conversation is null || string.IsNullOrWhiteSpace(conversation.ActiveBranchId))
            return null;

        string sourceBranchId = conversation.ActiveBranchId;

        var sourceTurns = await _dbContext.CopilotBranchTurns
            .Where(x => x.UserId == userId && x.ThreadId == threadId && x.BranchId == sourceBranchId)
            .OrderBy(x => x.Position)
            .ToListAsync(cancellationToken);

        var editedTurn = sourceTurns.FirstOrDefault(x => x.UserMessageId == userMessageId);

        if (editedTurn is null)
            return null;

        DateTime now = DateTime.UtcNow;
        string newBranchId = Guid.NewGuid().ToString("N");

        var branch = new CopilotConversationBranchRecord
        {
            UserId = userId,
            ThreadId = threadId,
            BranchId = newBranchId,
            ParentBranchId = sourceBranchId,
            BranchedFromUserMessageId = userMessageId,
            BranchedFromVersionNumber = editedTurn.VersionNumber,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.CopilotConversationBranches.Add(branch);

        foreach (var sourceTurn in sourceTurns.Where(x => x.Position < editedTurn.Position))
        {
            _dbContext.CopilotBranchTurns.Add(new CopilotBranchTurnRecord
            {
                UserId = userId,
                ThreadId = threadId,
                BranchId = newBranchId,
                UserMessageId = sourceTurn.UserMessageId,
                VersionNumber = sourceTurn.VersionNumber,
                Position = sourceTurn.Position,
                CreatedAt = now
            });
        }

        _dbContext.CopilotBranchTurns.Add(new CopilotBranchTurnRecord
        {
            UserId = userId,
            ThreadId = threadId,
            BranchId = newBranchId,
            UserMessageId = userMessageId,
            VersionNumber = newVersionNumber,
            Position = editedTurn.Position,
            CreatedAt = now
        });

        conversation.ActiveBranchId = newBranchId;
        conversation.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return branch;
    }

    public async Task<IReadOnlyList<CopilotBranchTurnRecord>> GetActiveBranchTurnsAsync(
        string threadId,
        CancellationToken cancellationToken = default)
    {
        int userId = GetRequiredUserId();

        var conversation = await _dbContext.CopilotConversations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.UserId == userId && x.ThreadId == threadId,
                cancellationToken);

        if (conversation is null || string.IsNullOrWhiteSpace(conversation.ActiveBranchId))
            return [];

        return await GetBranchTurnsAsync(threadId, conversation.ActiveBranchId, cancellationToken);
    }

    public async Task<IReadOnlyList<CopilotBranchTurnRecord>> GetBranchTurnsAsync(
        string threadId,
        string branchId,
        CancellationToken cancellationToken = default)
    {
        int userId = GetRequiredUserId();

        return await _dbContext.CopilotBranchTurns
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.ThreadId == threadId && x.BranchId == branchId)
            .OrderBy(x => x.Position)
            .ToListAsync(cancellationToken);
    }

    public async Task<CopilotBranchSnapshot?> GetBranchForVersionAsync(
    string threadId,
    string userMessageId,
    int versionNumber,
    CancellationToken cancellationToken = default)
    {
        int userId = GetRequiredUserId();

        var candidateBranchIds = await _dbContext.CopilotBranchTurns
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId &&
                x.ThreadId == threadId &&
                x.UserMessageId == userMessageId &&
                x.VersionNumber == versionNumber)
            .Select(x => x.BranchId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (candidateBranchIds.Count == 0)
            return null;

        /*
         * Pick the branch where this version first appeared.
         * Descendant branches may contain the same version as part
         * of their shared prefix.
         */
        var branch = await _dbContext.CopilotConversationBranches
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId &&
                x.ThreadId == threadId &&
                candidateBranchIds.Contains(x.BranchId))
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (branch is null)
            return null;

        var turns = await (
            from branchTurn in _dbContext.CopilotBranchTurns.AsNoTracking()
            join version in _dbContext.CopilotTurnVersions.AsNoTracking()
                on new
                {
                    branchTurn.UserId,
                    branchTurn.ThreadId,
                    branchTurn.UserMessageId,
                    branchTurn.VersionNumber
                }
                equals new
                {
                    version.UserId,
                    version.ThreadId,
                    version.UserMessageId,
                    version.VersionNumber
                }
            where
                branchTurn.UserId == userId &&
                branchTurn.ThreadId == threadId &&
                branchTurn.BranchId == branch.BranchId
            orderby branchTurn.Position
            select new CopilotBranchTurnSnapshot(
                branchTurn.UserMessageId,
                branchTurn.VersionNumber,
                branchTurn.Position,
                version.UserContent,
                version.AssistantMessageId,
                version.AssistantContent,
                version.Status,
                version.ActivitiesJson)
        ).ToListAsync(cancellationToken);

        return new CopilotBranchSnapshot(
            branch.BranchId,
            branch.ParentBranchId,
            branch.BranchedFromUserMessageId,
            branch.BranchedFromVersionNumber,
            turns);
    }

    public async Task<bool> SetActiveBranchAsync(
    string threadId,
    string branchId,
    CancellationToken cancellationToken = default)
    {
        int userId = GetRequiredUserId();

        bool exists = await _dbContext.CopilotConversationBranches.AnyAsync(
            x => x.UserId == userId && x.ThreadId == threadId && x.BranchId == branchId,
            cancellationToken);

        if (!exists) return false;

        var conversation = await _dbContext.CopilotConversations.SingleOrDefaultAsync(
            x => x.UserId == userId && x.ThreadId == threadId,
            cancellationToken);

        if (conversation is null) return false;

        conversation.ActiveBranchId = branchId;
        conversation.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task SyncBranchTurnsAsync(
        string branchId,
        string threadId,
        IReadOnlyList<string> activeUserMessageIds,
        CancellationToken cancellationToken)
    {
        int userId = GetRequiredUserId();

        var orderedIds = activeUserMessageIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (orderedIds.Count == 0)
            return;

        var turns = await _dbContext.CopilotTurns
            .Where(x => x.UserId == userId && x.ThreadId == threadId && orderedIds.Contains(x.UserMessageId))
            .ToDictionaryAsync(x => x.UserMessageId, cancellationToken);

        var existing = await _dbContext.CopilotBranchTurns
            .Where(x => x.UserId == userId && x.ThreadId == threadId && x.BranchId == branchId)
            .ToListAsync(cancellationToken);

        var existingIds = existing.Select(x => x.UserMessageId).ToHashSet(StringComparer.Ordinal);
        int nextPosition = existing.Count == 0 ? 1 : existing.Max(x => x.Position) + 1;
        DateTime now = DateTime.UtcNow;

        foreach (string messageId in orderedIds)
        {
            if (existingIds.Contains(messageId) || !turns.TryGetValue(messageId, out var turn))
                continue;

            _dbContext.CopilotBranchTurns.Add(new CopilotBranchTurnRecord
            {
                UserId = userId,
                ThreadId = threadId,
                BranchId = branchId,
                UserMessageId = messageId,
                VersionNumber = turn.CurrentVersionNumber,
                Position = nextPosition++,
                CreatedAt = now
            });
        }
    }

    private int GetRequiredUserId()
    {
        return _currentUserContext.UserId
            ?? throw new UnauthorizedAccessException("An authenticated user is required to access Copilot branches.");
    }
}