using Microsoft.EntityFrameworkCore;
using StudentManagement.AI.Workflows.Enrollment;
using StudentManagement.Core.Models;
using StudentManagement.Infrastructure.Hybrid;

namespace StudentManagementApp.WebApi.Workflows;

public sealed class EnrollmentWorkflowHistoryStore
    : IEnrollmentWorkflowHistoryStore
{
    private readonly IDbContextFactory<HybridDbContext>
        _dbContextFactory;

    public EnrollmentWorkflowHistoryStore(
        IDbContextFactory<HybridDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task AddAsync(
        string requestId,
        string eventType,
        string? executorId = null,
        long? durationMs = null,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var history =
            new EnrollmentWorkflowHistory
            {
                RequestId = requestId,
                EventType = eventType,
                ExecutorId = executorId,
                DurationMs = durationMs,
                Message = message,
                OccurredAt = DateTime.UtcNow
            };

        dbContext.EnrollmentWorkflowHistories.Add(
            history);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<IReadOnlyList<EnrollmentWorkflowHistory>>
    GetByRequestIdAsync(
        string requestId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await dbContext.EnrollmentWorkflowHistories
            .AsNoTracking()
            .Where(x => x.RequestId == requestId)
            .OrderBy(x => x.OccurredAt)
            .ToListAsync(cancellationToken);
    }
}