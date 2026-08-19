using Microsoft.Agents.AI.Workflows;
using StudentManagement.AI.Workflows.Enrollment.Models;

namespace StudentManagement.AI.Workflows.Enrollment.Executors;

public sealed class EnrollmentApprovalRejectedExecutor
    : Executor<
        EnrollmentApprovalResponse,
        EnrollmentWorkflowResult>
{
    public EnrollmentApprovalRejectedExecutor()
        : base("enrollment_approval_rejected")
    {
    }

    public override ValueTask<EnrollmentWorkflowResult> HandleAsync(
        EnrollmentApprovalResponse input,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(
            new EnrollmentWorkflowResult(
                Success: false,
                StudentId: input.StudentId,
                CourseId: input.CourseId,
                Message:
                    input.Reason ??
                    "The enrollment request was rejected."));
    }
}
