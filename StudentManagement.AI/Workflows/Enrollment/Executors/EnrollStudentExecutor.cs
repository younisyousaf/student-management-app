using Microsoft.Agents.AI.Workflows;
using StudentManagement.AI.Workflows.Enrollment.Models;
using StudentManagement.Core.Interfaces;

namespace StudentManagement.AI.Workflows.Enrollment.Executors;

public sealed class EnrollStudentExecutor
    : Executor<
        EnrollmentApprovalResponse,
        EnrollmentWorkflowResult>
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollStudentExecutor(
        IEnrollmentService enrollmentService)
        : base("enroll_student")
    {
        _enrollmentService = enrollmentService;
    }

    public override ValueTask<EnrollmentWorkflowResult> HandleAsync(
        EnrollmentApprovalResponse input,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        _enrollmentService.EnrollStudent(
            input.StudentId,
            input.CourseId);

        return ValueTask.FromResult(
            new EnrollmentWorkflowResult(
                Success: true,
                StudentId: input.StudentId,
                CourseId: input.CourseId,
                Message: "Student enrolled successfully."));
    }
}
