using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Checkpointing;
using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Models;
using StudentManagement.Infrastructure.Hybrid;
using System.Text.Json;

namespace StudentManagementApp.WebApi.Workflows;

public sealed class SqlWorkflowCheckpointStore
    : ICheckpointStore<JsonElement>
{
    private readonly IDbContextFactory<HybridDbContext> _dbContextFactory;

    public SqlWorkflowCheckpointStore(
        IDbContextFactory<HybridDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async ValueTask<CheckpointInfo> CreateCheckpointAsync(
        string sessionId,
        JsonElement value,
        CheckpointInfo? parent = null)
    {
        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync();

        var checkpointId =
            Guid.NewGuid().ToString("N");

        var entity =
            new WorkflowCheckpointRecord
            {
                SessionId = sessionId,
                CheckpointId = checkpointId,
                ParentCheckpointId =
                    parent?.CheckpointId,

                CheckpointData =
                    value.GetRawText(),

                CreatedAt =
                    DateTime.UtcNow
            };

        dbContext.WorkflowCheckpoints.Add(entity);

        await dbContext.SaveChangesAsync();

        return new CheckpointInfo(
            sessionId,
            checkpointId);
    }

    public async ValueTask<JsonElement> RetrieveCheckpointAsync(
        string sessionId,
        CheckpointInfo key)
    {
        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync();

        var entity =
            await dbContext.WorkflowCheckpoints
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.SessionId == sessionId &&
                        x.CheckpointId == key.CheckpointId);

        if (entity is null)
        {
            throw new KeyNotFoundException(
                $"Workflow checkpoint '{key.CheckpointId}' was not found for session '{sessionId}'.");
        }

        using var document =
            JsonDocument.Parse(
                entity.CheckpointData);

        return document.RootElement.Clone();
    }

    public async ValueTask<IEnumerable<CheckpointInfo>> RetrieveIndexAsync(
        string sessionId,
        CheckpointInfo? withParent = null)
    {
        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync();

        var query =
            dbContext.WorkflowCheckpoints
                .AsNoTracking()
                .Where(
                    x => x.SessionId == sessionId);

        if (withParent is not null)
        {
            query =
                query.Where(
                    x =>
                        x.ParentCheckpointId ==
                        withParent.CheckpointId);
        }

        var checkpoints =
            await query
                .OrderBy(x => x.CreatedAt)
                .Select(
                    x => new
                    {
                        x.SessionId,
                        x.CheckpointId
                    })
                .ToListAsync();

        return checkpoints
            .Select(
                x => new CheckpointInfo(
                    x.SessionId,
                    x.CheckpointId))
            .ToList();
    }
}
