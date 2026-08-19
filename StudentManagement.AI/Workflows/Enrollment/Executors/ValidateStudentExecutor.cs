using Microsoft.Agents.AI.Workflows;
using StudentManagement.AI.Workflows.Enrollment.Models;
using StudentManagement.Core.Interfaces;

namespace StudentManagement.AI.Workflows.Enrollment.Executors;

public sealed class ValidateStudentExecutor
    : Executor<EnrollmentWorkflowRequest, EnrollmentWorkflowRequest>
{
    private readonly IStudentService _studentService;

    public ValidateStudentExecutor(
        IStudentService studentService)
        : base("validate_student")
    {
        _studentService = studentService;
    }

    public override ValueTask<EnrollmentWorkflowRequest> HandleAsync(
        EnrollmentWorkflowRequest input,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var student =
            _studentService.GetStudentById(
                input.StudentId);

        if (student is null)
        {
            throw new KeyNotFoundException(
                $"Student with ID {input.StudentId} was not found.");
        }

        return ValueTask.FromResult(input);
    }
}
