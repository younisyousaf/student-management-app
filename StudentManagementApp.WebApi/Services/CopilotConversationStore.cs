using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;
using StudentManagement.Infrastructure.Hybrid;

namespace StudentManagementApp.WebApi.Services;

public sealed class CopilotConversationStore
{
    private const string DefaultTitle =
        "New conversation";

    private readonly HybridDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public CopilotConversationStore(
        HybridDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<
     PaginatedResult<CopilotConversationRecord>>
     GetPageAsync(
         int pageNumber,
         int pageSize,
         CancellationToken cancellationToken = default)
    {
        int userId =
            GetRequiredUserId();

        var query =
            _dbContext
                .CopilotConversations
                .AsNoTracking()
                .Where(
                    conversation =>
                        conversation.UserId ==
                        userId);

        int totalCount =
            await query.CountAsync(
                cancellationToken);

        int skip =
            (pageNumber - 1) *
            pageSize;

        var conversations =
            await query
                .OrderByDescending(
                    conversation =>
                        conversation.UpdatedAt)
                .ThenByDescending(
                    conversation =>
                        conversation.Id)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(
                    cancellationToken);

        return new PaginatedResult<
            CopilotConversationRecord>(
                conversations,
                pageNumber,
                pageSize,
                totalCount);
    }

    public async Task<CopilotConversationRecord>
    EnsureConversationAsync(
        string threadId,
        string? titleCandidate,
        CancellationToken cancellationToken = default)
    {
        int userId =
            GetRequiredUserId();

        var conversation =
            await _dbContext
                .CopilotConversations
                .SingleOrDefaultAsync(
                    conversation =>
                        conversation.UserId == userId &&
                        conversation.ThreadId == threadId,
                    cancellationToken);

        DateTime now =
            DateTime.UtcNow;

        if (conversation is null)
        {
            conversation =
                new CopilotConversationRecord
                {
                    UserId = userId,

                    ThreadId =
                        threadId,

                    Title =
                        CreateTitle(
                            titleCandidate),

                    /*
                     * No run has completed yet.
                     */
                    LastRunId = null,

                    CreatedAt =
                        now,

                    UpdatedAt =
                        now
                };

            _dbContext
                .CopilotConversations
                .Add(conversation);
        }
        else
        {
            /*
             * The user has started another turn,
             * so move this conversation to the top
             * even if the AI run is later stopped.
             */
            conversation.UpdatedAt =
                now;

            /*
             * Do NOT change LastRunId here.
             * It represents the latest completed run.
             */
            if (
                conversation.Title ==
                    DefaultTitle &&
                !string.IsNullOrWhiteSpace(
                    titleCandidate)
            )
            {
                conversation.Title =
                    CreateTitle(
                        titleCandidate);
            }
        }

        await _dbContext
            .SaveChangesAsync(
                cancellationToken);

        return conversation;
    }

    public async Task<CopilotConversationRecord>
        SaveRunAsync(
            string threadId,
            string runId,
            string? titleCandidate,
            CancellationToken cancellationToken = default)
    {
        int userId =
            GetRequiredUserId();

        var conversation =
            await _dbContext
                .CopilotConversations
                .SingleOrDefaultAsync(
                    x =>
                        x.UserId == userId &&
                        x.ThreadId == threadId,
                    cancellationToken);

        DateTime now =
            DateTime.UtcNow;

        if (conversation is null)
        {
            conversation =
                new CopilotConversationRecord
                {
                    UserId = userId,

                    ThreadId =
                        threadId,

                    Title =
                        CreateTitle(
                            titleCandidate),

                    LastRunId =
                        runId,

                    CreatedAt =
                        now,

                    UpdatedAt =
                        now
                };

            _dbContext
                .CopilotConversations
                .Add(conversation);
        }
        else
        {
            conversation.LastRunId =
                runId;

            conversation.UpdatedAt =
                now;

            /*
             * Normally the title comes from the
             * first user message.
             *
             * This also allows a placeholder title
             * to be corrected later.
             */
            if (
                conversation.Title ==
                    DefaultTitle &&
                !string.IsNullOrWhiteSpace(
                    titleCandidate)
            )
            {
                conversation.Title =
                    CreateTitle(
                        titleCandidate);
            }
        }

        await _dbContext
            .SaveChangesAsync(
                cancellationToken);

        return conversation;
    }

    public async Task<CopilotConversationRecord?>
    GetByThreadIdAsync(
        string threadId,
        CancellationToken cancellationToken = default)
    {
        int userId =
            GetRequiredUserId();

        return await _dbContext
            .CopilotConversations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                conversation =>
                    conversation.UserId == userId &&
                    conversation.ThreadId == threadId,
                cancellationToken);
    }

    public async Task<CopilotConversationRecord?>
        RenameAsync(
            string threadId,
            string title,
            CancellationToken cancellationToken = default)
    {
        int userId =
            GetRequiredUserId();

        var conversation =
            await _dbContext
                .CopilotConversations
                .SingleOrDefaultAsync(
                    conversation =>
                        conversation.UserId == userId &&
                        conversation.ThreadId == threadId,
                    cancellationToken);

        if (conversation is null)
        {
            return null;
        }

        conversation.Title =
            NormalizeTitle(title);

        await _dbContext
            .SaveChangesAsync(
                cancellationToken);

        return conversation;
    }

    public async Task<bool>
        DeleteAsync(
            string threadId,
            CancellationToken cancellationToken = default)
    {
        int userId =
            GetRequiredUserId();

        var conversation =
            await _dbContext
                .CopilotConversations
                .SingleOrDefaultAsync(
                    conversation =>
                        conversation.UserId == userId &&
                        conversation.ThreadId == threadId,
                    cancellationToken);

        if (conversation is null)
        {
            return false;
        }

        _dbContext
            .CopilotConversations
            .Remove(conversation);

        await _dbContext
            .SaveChangesAsync(
                cancellationToken);

        return true;
    }

    private static string NormalizeTitle(
        string title)
    {
        string normalized =
            string.Join(
                " ",
                title.Split(
                    [' ', '\r', '\n', '\t'],
                    StringSplitOptions.RemoveEmptyEntries));

        const int maxLength = 80;

        if (normalized.Length <= maxLength)
        {
            return normalized;
        }

        return
            normalized[..77] +
            "...";
    }

    private int GetRequiredUserId()
    {
        return _currentUserContext.UserId
            ?? throw new UnauthorizedAccessException(
                "An authenticated user is required to access Copilot conversations.");
    }

    private static string CreateTitle(
        string? titleCandidate)
    {
        if (
            string.IsNullOrWhiteSpace(
                titleCandidate)
        )
        {
            return DefaultTitle;
        }

        string normalized =
            string.Join(
                " ",
                titleCandidate
                    .Split(
                        [' ', '\r', '\n', '\t'],
                        StringSplitOptions
                            .RemoveEmptyEntries));

        const int maxLength = 80;

        if (
            normalized.Length <=
            maxLength
        )
        {
            return normalized;
        }

        return
            normalized[..77] +
            "...";
    }
}