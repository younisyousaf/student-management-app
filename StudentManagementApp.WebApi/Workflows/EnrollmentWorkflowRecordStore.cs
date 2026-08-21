using Microsoft.EntityFrameworkCore;
using StudentManagement.AI.Common.Models;
using StudentManagement.AI.Workflows.Enrollment;
using StudentManagement.AI.Workflows.Enrollment.Models;
using StudentManagement.Core.Enums;
using StudentManagement.Core.Models;
using StudentManagement.Infrastructure.Hybrid;

namespace StudentManagementApp.WebApi.Workflows;

public sealed class EnrollmentWorkflowRecordStore
    : IEnrollmentWorkflowRecordStore
{
    private readonly IDbContextFactory<HybridDbContext> _dbContextFactory;

    public EnrollmentWorkflowRecordStore(
        IDbContextFactory<HybridDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task SavePendingAsync(
        string requestId,
        int studentId,
        int courseId,
        string checkpointRunId,
        string checkpointId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var existing =
            await dbContext.EnrollmentWorkflowRecords
                .SingleOrDefaultAsync(
                    x => x.RequestId == requestId,
                    cancellationToken);

        if (existing is null)
        {
            var entity =
                new EnrollmentWorkflowRecord
                {
                    RequestId = requestId,
                    StudentId = studentId,
                    CourseId = courseId,
                    Status =
                        EnrollmentWorkflowStatus.WaitingForApproval,
                    Approved = null,
                    ActiveKey =
                        $"{studentId}:{courseId}",
                    CheckpointRunId = checkpointRunId,
                    CheckpointId = checkpointId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

            dbContext.EnrollmentWorkflowRecords.Add(entity);
        }
        else
        {
            existing.StudentId = studentId;
            existing.CourseId = courseId;
            existing.Status =
                EnrollmentWorkflowStatus.WaitingForApproval;
            existing.Approved = null;
            existing.ActiveKey =
                $"{studentId}:{courseId}";
            existing.CheckpointRunId = checkpointRunId;
            existing.CheckpointId = checkpointId;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.CompletedAt = null;
        }

        try
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException(
                $"An active enrollment workflow already exists for student '{studentId}' and course '{courseId}'.",
                ex);
        }
    }

    public async Task<EnrollmentWorkflowRecord?> GetByRequestIdAsync(
        string requestId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await dbContext.EnrollmentWorkflowRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.RequestId == requestId,
                cancellationToken);
    }

    public async Task<bool> TryBeginProcessingAsync(
        string requestId,
        bool approved,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        int affectedRows =
            await dbContext.EnrollmentWorkflowRecords
                .Where(x =>
                    x.RequestId == requestId &&
                    x.Status ==
                        EnrollmentWorkflowStatus.WaitingForApproval)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            x => x.Status,
                            EnrollmentWorkflowStatus.Processing)
                        .SetProperty(
                            x => x.Approved,
                            approved)
                        .SetProperty(
                            x => x.UpdatedAt,
                            DateTime.UtcNow),
                    cancellationToken);

        return affectedRows == 1;
    }

    public async Task<bool> TryMarkCompletedFromProcessingAsync(
        string requestId,
        EnrollmentWorkflowStatus finalStatus,
        CancellationToken cancellationToken = default)
    {
        if (finalStatus != EnrollmentWorkflowStatus.Completed &&
            finalStatus != EnrollmentWorkflowStatus.Rejected)
        {
            throw new ArgumentException(
                "Final status must be Completed or Rejected.",
                nameof(finalStatus));
        }

        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var now = DateTime.UtcNow;

        int affectedRows =
            await dbContext.EnrollmentWorkflowRecords
                .Where(x =>
                    x.RequestId == requestId &&
                    x.Status == EnrollmentWorkflowStatus.Processing)
                .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        x => x.Status,
                        finalStatus)
                    .SetProperty(
                        x => x.UpdatedAt,
                        now)
                    .SetProperty(
                        x => x.CompletedAt,
                        now)
                    .SetProperty(
                        x => x.ActiveKey,
                    (string?)null),
                cancellationToken);

        return affectedRows == 1;
    }

    public async Task<bool> TryReconcileAsCompletedAsync(
        string requestId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var now = DateTime.UtcNow;

        int affectedRows =
            await dbContext.EnrollmentWorkflowRecords
                .Where(x =>
                    x.RequestId == requestId &&
                    (
                        x.Status == EnrollmentWorkflowStatus.Failed ||
                        x.Status == EnrollmentWorkflowStatus.Interrupted ||
                        x.Status == EnrollmentWorkflowStatus.ReadyForRetry
                    ))
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            x => x.Status,
                            EnrollmentWorkflowStatus.Completed)
                        .SetProperty(
                            x => x.UpdatedAt,
                            now)
                        .SetProperty(
                            x => x.CompletedAt,
                            now)
                        .SetProperty(
                            x => x.ActiveKey,
                            (string?)null),
                    cancellationToken);

        return affectedRows == 1;
    }

    public async Task<bool> TryReconcileAsRejectedAsync(
        string requestId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var now = DateTime.UtcNow;

        int affectedRows =
            await dbContext.EnrollmentWorkflowRecords
                .Where(x =>
                    x.RequestId == requestId &&
                    (
                        x.Status == EnrollmentWorkflowStatus.Failed ||
                        x.Status == EnrollmentWorkflowStatus.Interrupted
                    ) &&
                    x.Approved == false)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            x => x.Status,
                            EnrollmentWorkflowStatus.Rejected)
                        .SetProperty(
                            x => x.UpdatedAt,
                            now)
                        .SetProperty(
                            x => x.CompletedAt,
                            now)
                        .SetProperty(
                            x => x.ActiveKey,
                            (string?)null),
                    cancellationToken);

        return affectedRows == 1;
    }

    public async Task MarkFailedAsync(
        string requestId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        int affectedRows =
            await dbContext.EnrollmentWorkflowRecords
                .Where(x =>
                    x.RequestId == requestId &&
                    x.Status == EnrollmentWorkflowStatus.Processing)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            x => x.Status,
                            EnrollmentWorkflowStatus.Failed)
                        .SetProperty(
                            x => x.UpdatedAt,
                            DateTime.UtcNow),
                    cancellationToken);

        if (affectedRows == 0)
        {
            throw new InvalidOperationException(
                $"Workflow '{requestId}' could not be marked as failed.");
        }
    }

    public async Task MarkInterruptedAsync(
        string requestId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        int affectedRows =
            await dbContext.EnrollmentWorkflowRecords
                .Where(x =>
                    x.RequestId == requestId &&
                    x.Status == EnrollmentWorkflowStatus.Processing)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            x => x.Status,
                            EnrollmentWorkflowStatus.Interrupted)
                        .SetProperty(
                            x => x.UpdatedAt,
                            DateTime.UtcNow),
                    cancellationToken);

        if (affectedRows == 0)
        {
            throw new InvalidOperationException(
                $"Workflow '{requestId}' could not be marked as interrupted.");
        }
    }

    public async Task<bool> MarkReadyForRetryAsync(
    string requestId,
    CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        int affectedRows =
            await dbContext.EnrollmentWorkflowRecords
                .Where(x =>
                    x.RequestId == requestId &&
                    (
                        x.Status == EnrollmentWorkflowStatus.Failed ||
                        x.Status == EnrollmentWorkflowStatus.Interrupted
                    ))
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            x => x.Status,
                            EnrollmentWorkflowStatus.ReadyForRetry)
                        .SetProperty(
                            x => x.UpdatedAt,
                            DateTime.UtcNow)
                        .SetProperty(
                            x => x.CompletedAt,
                            (DateTime?)null),
                    cancellationToken);

        return affectedRows == 1;
    }

    public async Task<bool> TryBeginRetryAsync(
    string requestId,
    CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        int affectedRows =
            await dbContext.EnrollmentWorkflowRecords
                .Where(x =>
                    x.RequestId == requestId &&
                    x.Status ==
                        EnrollmentWorkflowStatus.ReadyForRetry)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            x => x.Status,
                            EnrollmentWorkflowStatus.Processing)
                        .SetProperty(
                            x => x.UpdatedAt,
                            DateTime.UtcNow)
                        .SetProperty(
                            x => x.CompletedAt,
                            (DateTime?)null),
                    cancellationToken);

        return affectedRows == 1;
    }

    public async Task<IReadOnlyList<EnrollmentWorkflowRecord>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await dbContext.EnrollmentWorkflowRecords
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> MarkStaleProcessingAsInterruptedAsync(
    DateTime staleBeforeUtc,
    CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var now = DateTime.UtcNow;

        int affectedRows =
            await dbContext.EnrollmentWorkflowRecords
                .Where(x =>
                    x.Status == EnrollmentWorkflowStatus.Processing &&
                    x.UpdatedAt < staleBeforeUtc)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            x => x.Status,
                            EnrollmentWorkflowStatus.Interrupted)
                        .SetProperty(
                            x => x.UpdatedAt,
                            now)
                        .SetProperty(
                            x => x.CompletedAt,
                            (DateTime?)null),
                    cancellationToken);

        return affectedRows;
    }

    public async Task<EnrollmentWorkflowRecord?> GetActiveByStudentAndCourseAsync(
    int studentId,
    int courseId,
    CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await dbContext.EnrollmentWorkflowRecords
            .AsNoTracking()
            .Where(x =>
                x.StudentId == studentId &&
                x.CourseId == courseId &&
                (
                    x.Status == EnrollmentWorkflowStatus.WaitingForApproval ||
                    x.Status == EnrollmentWorkflowStatus.Processing ||
                    x.Status == EnrollmentWorkflowStatus.ReadyForRetry ||
                    x.Status == EnrollmentWorkflowStatus.Failed ||
                    x.Status == EnrollmentWorkflowStatus.Interrupted
                ))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<EnrollmentWorkflowRecord>> QueryAsync(
    EnrollmentWorkflowQuery query,
    CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        IQueryable<EnrollmentWorkflowRecord> workflows =
            dbContext.EnrollmentWorkflowRecords
                .AsNoTracking();

        if (query.Status.HasValue)
        {
            workflows =
                workflows.Where(x =>
                    x.Status == query.Status.Value);
        }

        if (query.StudentId.HasValue)
        {
            workflows =
                workflows.Where(x =>
                    x.StudentId == query.StudentId.Value);
        }

        if (query.CourseId.HasValue)
        {
            workflows =
                workflows.Where(x =>
                    x.CourseId == query.CourseId.Value);
        }

        int totalCount =
            await workflows.CountAsync(
                cancellationToken);

        int totalPages =
            totalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    totalCount / (double)query.PageSize);

        var items =
            await workflows
                .OrderByDescending(x => x.CreatedAt)
                .Skip(
                    (query.Page - 1) *
                    query.PageSize)
                .Take(
                    query.PageSize)
                .ToListAsync(
                    cancellationToken);

        return new PagedResult<EnrollmentWorkflowRecord>(
            Items: items,
            Page: query.Page,
            PageSize: query.PageSize,
            TotalCount: totalCount,
            TotalPages: totalPages);
    }
}