using Microsoft.Agents.AI.Workflows;
using StudentManagement.AI.Workflows.Enrollment.Models;
using StudentManagement.Core.Interfaces;

namespace StudentManagement.AI.Workflows.Enrollment.Executors;

public sealed class ValidateCourseExecutor
    : Executor<EnrollmentWorkflowRequest, EnrollmentWorkflowRequest>
{
    private readonly ICourseService _courseService;

    public ValidateCourseExecutor(
        ICourseService courseService)
        : base("validate_course")
    {
        _courseService = courseService;
    }

    public override ValueTask<EnrollmentWorkflowRequest> HandleAsync(
        EnrollmentWorkflowRequest input,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var course =
            _courseService.GetCourseById(
                input.CourseId);

        if (course is null)
        {
            throw new KeyNotFoundException(
                $"Course with ID {input.CourseId} was not found.");
        }

        return ValueTask.FromResult(input);
    }
}
