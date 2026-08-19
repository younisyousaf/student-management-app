using Microsoft.EntityFrameworkCore;
using StudentManagement.AI.Workflows.Enrollment;
using StudentManagement.Core.Enums;
using StudentManagement.Core.Models;
using StudentManagement.Infrastructure.Hybrid;

namespace StudentManagementApp.WebApi.Workflows;

public sealed class EnrollmentWorkflowRecordStore : IEnrollmentWorkflowRecordStore
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
                    Status = EnrollmentWorkflowStatus.WaitingForApproval,
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
            existing.Status = EnrollmentWorkflowStatus.WaitingForApproval;
            existing.CheckpointRunId = checkpointRunId;
            existing.CheckpointId = checkpointId;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.CompletedAt = null;
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);
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

    public async Task MarkCompletedAsync(
        string requestId,
        EnrollmentWorkflowStatus status,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var entity =
            await dbContext.EnrollmentWorkflowRecords
                .SingleOrDefaultAsync(
                    x => x.RequestId == requestId,
                    cancellationToken);

        if (entity is null)
        {
            return;
        }

        entity.Status = status;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.CompletedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
