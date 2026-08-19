using Microsoft.Agents.AI.Workflows;
using StudentManagement.AI.Workflows.Enrollment.Executors;
using StudentManagement.AI.Workflows.Enrollment.Models;

namespace StudentManagement.AI.Workflows.Enrollment;

public static class EnrollmentWorkflowFactory
{
    public static Workflow Create(
        ValidateStudentExecutor validateStudent,
        ValidateCourseExecutor validateCourse,
        CheckExistingEnrollmentExecutor checkExistingEnrollment,
        EnrollmentRejectedExecutor enrollmentRejected,
        PrepareEnrollmentApprovalExecutor prepareApproval,
        EnrollmentApprovalRejectedExecutor approvalRejected,
        EnrollStudentExecutor enrollStudent)
    {
        var approvalPort =
           RequestPort.Create<
               EnrollmentApprovalRequest,
               EnrollmentApprovalResponse>(
               "EnrollmentApproval");

        var builder =
            new WorkflowBuilder(
                validateStudent);

        builder.AddEdge(
            validateStudent,
            validateCourse);

        builder.AddEdge(
            validateCourse,
            checkExistingEnrollment);

        builder.AddEdge<EnrollmentValidationResult>(
            checkExistingEnrollment,
            enrollmentRejected,
            condition: result =>
                result is not null &&
                !result.CanEnroll);

        builder.AddEdge<EnrollmentValidationResult>(
            checkExistingEnrollment,
            prepareApproval,
            condition: result => 
                result is not null &&
                result.CanEnroll);

        builder.AddEdge(
            prepareApproval,
            approvalPort);

        builder.AddEdge<EnrollmentApprovalResponse>(
            approvalPort,
            approvalRejected,
            condition: response =>
                response is not null &&
                !response.Approved);

        builder.AddEdge<EnrollmentApprovalResponse>(
            approvalPort,
            enrollStudent,
            condition: response =>
                response is not null &&
                response.Approved);

        builder.WithOutputFrom(
            enrollmentRejected);

        builder.WithOutputFrom(
            approvalRejected);

        builder.WithOutputFrom(
            enrollStudent);

        return builder.Build();
    }
}
