using Microsoft.Agents.AI.Workflows;
using StudentManagement.AI.Workflows.Enrollment.Models;
using StudentManagement.Core.Interfaces;

namespace StudentManagement.AI.Workflows.Enrollment.Executors;

public sealed class CheckExistingEnrollmentExecutor
    : Executor<
        EnrollmentWorkflowRequest,
        EnrollmentValidationResult>
{
    private readonly IEnrollmentService _enrollmentService;

    public CheckExistingEnrollmentExecutor(
        IEnrollmentService enrollmentService)
        : base("check_existing_enrollment")
    {
        _enrollmentService = enrollmentService;
    }

    public override ValueTask<EnrollmentValidationResult> HandleAsync(
        EnrollmentWorkflowRequest input,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var enrollments =
            _enrollmentService
                .GetEnrollmentsByStudent(
                    input.StudentId);

        bool alreadyEnrolled =
        enrollments.Any(
        enrollment =>
            enrollment.CourseId == input.CourseId &&
            string.Equals(
                enrollment.Status,
                "Active",
                StringComparison.OrdinalIgnoreCase));

        if (alreadyEnrolled)
        {
            return ValueTask.FromResult(
                new EnrollmentValidationResult(
                    input.StudentId,
                    input.CourseId,
                    CanEnroll: false,
                    Message:
                        "The student is already actively enrolled in this course."));
        }

        return ValueTask.FromResult(
            new EnrollmentValidationResult(
                input.StudentId,
                input.CourseId,
                CanEnroll: true,
                Message:
                    "The student is eligible to continue to enrollment approval."));
    }
}
