using StudentManagement.AI.Common.Models;
using StudentManagement.AI.Workflows.Enrollment.Models;
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

    Task<EnrollmentWorkflowRecord?> GetActiveByStudentAndCourseAsync(
        int studentId,
        int courseId,
        CancellationToken cancellationToken = default);

    Task<bool> TryBeginProcessingAsync(
        string requestId,
        bool approved,
        CancellationToken cancellationToken = default);

    Task<bool> TryMarkCompletedFromProcessingAsync(
        string requestId,
        EnrollmentWorkflowStatus finalStatus,
        CancellationToken cancellationToken = default);

    Task<bool> TryReconcileAsCompletedAsync(
        string requestId,
        CancellationToken cancellationToken = default);

    Task<bool> TryReconcileAsRejectedAsync(
        string requestId,
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

    Task<bool> TryBeginRetryAsync(
        string requestId,
        CancellationToken cancellationToken = default);

    Task<int> MarkStaleProcessingAsInterruptedAsync(
        DateTime staleBeforeUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnrollmentWorkflowRecord>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResult<EnrollmentWorkflowRecord>> QueryAsync(
        EnrollmentWorkflowQuery query,
        CancellationToken cancellationToken = default);
}