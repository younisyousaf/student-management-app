using StudentManagement.Core.Models;

namespace StudentManagement.AI.Workflows.Enrollment;

public interface IEnrollmentWorkflowHistoryStore
{
    Task AddAsync(
        string requestId,
        string eventType,
        string? executorId = null,
        long? durationMs = null,
        string? message = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnrollmentWorkflowHistory>> GetByRequestIdAsync(
        string requestId,
        CancellationToken cancellationToken = default);
}