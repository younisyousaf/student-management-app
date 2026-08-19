using Microsoft.Agents.AI.Workflows;
using StudentManagement.AI.Workflows.Enrollment.Models;

namespace StudentManagement.AI.Workflows.Enrollment.Executors;

public sealed class EnrollmentRejectedExecutor
    : Executor<
        EnrollmentValidationResult,
        EnrollmentWorkflowResult>
{
    public EnrollmentRejectedExecutor()
        : base("enrollment_rejected")
    {
    }

    public override ValueTask<EnrollmentWorkflowResult> HandleAsync(
        EnrollmentValidationResult input,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(
            new EnrollmentWorkflowResult(
                Success: false,
                StudentId: input.StudentId,
                CourseId: input.CourseId,
                Message: input.Message));
    }
}
