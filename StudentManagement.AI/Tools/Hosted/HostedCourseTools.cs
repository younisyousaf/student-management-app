using System.ComponentModel;

namespace StudentManagement.AI.Tools.Hosted;

public sealed class HostedCourseTools
{
    private readonly ScopedToolExecutor _executor;

    public HostedCourseTools(
        ScopedToolExecutor executor)
    {
        _executor = executor;
    }

    [Description(
        "Get a single course by its exact course code. " +
        "Always check Found first.")]
    public CourseLookupResult GetCourseByCode(
        [Description("The exact course code.")]
        string code)
    {
        return _executor.Execute<
            CourseTools,
            CourseLookupResult>(
                tools =>
                    tools.GetCourseByCode(code));
    }

    [Description(
        "List all courses currently offered, including fee and duration.")]
    public IReadOnlyList<CourseSummary> GetAllCourses()
    {
        return _executor.Execute<
            CourseTools,
            IReadOnlyList<CourseSummary>>(
                tools =>
                    tools.GetAllCourses()
                        .ToList());
    }

    [Description(
        "Find a course by its exact internal course ID. " +
        "Always check Found first.")]
    public CourseLookupResult GetCourseById(
        [Description("The exact internal course ID.")]
        int courseId)
    {
        return _executor.Execute<
            CourseTools,
            CourseLookupResult>(
                tools =>
                    tools.GetCourseById(
                        courseId));
    }

    [Description(
        "Update one or more details of an existing course. " +
        "The exact course must be verified first. " +
        "This modifies course data and requires human approval.")]
    public string UpdateCourseDetails(
        [Description("The exact internal course ID.")]
        int courseId,

        [Description(
            "New course name, or null to preserve the current value.")]
        string? name = null,

        [Description(
            "New description, or null to preserve the current value.")]
        string? description = null,

        [Description(
            "New duration in months, or null to preserve the current value.")]
        int? durationMonths = null)
    {
        return _executor.Execute<
            CourseTools,
            string>(
                tools =>
                    tools.UpdateCourseDetails(
                        courseId,
                        name,
                        description,
                        durationMonths));
    }

    [Description(
        "Update the fee amount of an existing course. " +
        "The exact course must be verified first. " +
        "This modifies pricing and requires human approval.")]
    public string UpdateCoursePricing(
        [Description("The exact internal course ID.")]
        int courseId,

        [Description("The new course fee amount.")]
        decimal newFeeAmount)
    {
        return _executor.Execute<
            CourseTools,
            string>(
                tools =>
                    tools.UpdateCoursePricing(
                        courseId,
                        newFeeAmount));
    }

    [Description(
        "Permanently remove an existing course. " +
        "The exact course must be verified first. " +
        "This is destructive and requires human approval.")]
    public string RemoveCourse(
        [Description("The exact internal course ID.")]
        int courseId)
    {
        return _executor.Execute<
            CourseTools,
            string>(
                tools =>
                    tools.RemoveCourse(
                        courseId));
    }
}
