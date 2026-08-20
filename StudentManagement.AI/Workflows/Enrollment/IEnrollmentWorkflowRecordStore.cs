using StudentManagement.Core.Enums;
using StudentManagement.Core.Models;

namespace StudentManagement.AI.Workflows.Enrollment;

public interface IEnrollmentWorkflowRecordStore
{
    Task SavePendingAsync(
        string requestId,
        int studentId,
        int courseId,
        string checkpointRunId,
        string checkpointId,
        CancellationToken cancellationToken = default);

    Task<EnrollmentWorkflowRecord?> GetByRequestIdAsync(
        string requestId,
        CancellationToken cancellationToken = default);

    Task MarkCompletedAsync(
        string requestId,
        EnrollmentWorkflowStatus status,
        CancellationToken cancellationToken = default);

    Task<bool> TryBeginProcessingAsync(
        string requestId,
        bool approved,
        CancellationToken cancellationToken = default);

    Task MarkFailedAsync(
        string requestId,
        CancellationToken cancellationToken = default);

    Task MarkInterruptedAsync(
        string requestId,
        CancellationToken cancellationToken = default);

    Task<bool> MarkReadyForRetryAsync(
        string requestId,
        CancellationToken cancellationToken = default);
}
