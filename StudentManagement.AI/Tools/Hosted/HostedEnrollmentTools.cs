using System.ComponentModel;

namespace StudentManagement.AI.Tools.Hosted;

public sealed class HostedEnrollmentTools
{
    private readonly ScopedToolExecutor _executor;

    public HostedEnrollmentTools(
        ScopedToolExecutor executor)
    {
        _executor = executor;
    }

    [Description(
        "Get all enrollments for a specific student by internal student ID.")]
    public IReadOnlyList<EnrollmentSummary> GetEnrollmentsByStudent(
        [Description("The student's internal numeric ID.")]
        int studentId)
    {
        return _executor.Execute<
            EnrollmentTools,
            IReadOnlyList<EnrollmentSummary>>(
                tools =>
                    tools.GetEnrollmentsByStudent(studentId)
                        .ToList());
    }

    [Description(
        "Get a single enrollment record by its enrollment ID. " +
        "Always check Found first.")]
    public EnrollmentLookupResult GetEnrollmentById(
        [Description("The enrollment record ID.")]
        int enrollmentId)
    {
        return _executor.Execute<
            EnrollmentTools,
            EnrollmentLookupResult>(
                tools =>
                    tools.GetEnrollmentById(
                        enrollmentId));
    }

    [Description(
        "Enroll a student in a course. " +
        "This modifies application data and requires human approval.")]
    public string EnrollStudent(
        [Description("The exact internal student ID.")]
        int studentId,

        [Description("The exact internal course ID.")]
        int courseId)
    {
        return _executor.Execute<
            EnrollmentTools,
            string>(
                tools =>
                    tools.EnrollStudent(
                        studentId,
                        courseId));
    }

    [Description(
        "Drop an existing enrollment. " +
        "This modifies application data and requires human approval.")]
    public string DropCourse(
        [Description("The exact enrollment record ID.")]
        int enrollmentId)
    {
        return _executor.Execute<
            EnrollmentTools,
            string>(
                tools =>
                    tools.DropCourse(
                        enrollmentId));
    }

    [Description(
        "Mark an existing enrollment as completed. " +
        "This modifies application data and requires human approval.")]
    public string CompleteCourse(
        [Description("The exact enrollment record ID.")]
        int enrollmentId)
    {
        return _executor.Execute<
            EnrollmentTools,
            string>(
                tools =>
                    tools.CompleteCourse(
                        enrollmentId));
    }
}
