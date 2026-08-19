using Microsoft.Agents.AI.Workflows;
using StudentManagement.AI.Workflows.Enrollment.Models;

namespace StudentManagement.AI.Workflows.Enrollment.Executors;

public sealed class PrepareEnrollmentApprovalExecutor
    : Executor<
        EnrollmentValidationResult,
        EnrollmentApprovalRequest>
{
    public PrepareEnrollmentApprovalExecutor()
        : base("prepare_enrollment_approval")
    {
    }

    public override ValueTask<EnrollmentApprovalRequest> HandleAsync(
        EnrollmentValidationResult input,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(
            new EnrollmentApprovalRequest(
                StudentId: input.StudentId,
                CourseId: input.CourseId,
                Message:
                    $"Approve enrollment of student ID {input.StudentId} " +
                    $"into course ID {input.CourseId}?"));
    }
}
