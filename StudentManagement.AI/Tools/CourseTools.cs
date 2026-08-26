using System.ComponentModel;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;

namespace StudentManagement.AI.Tools;

public record CourseSummary(int Id, string Code, string Name, string? Description, int DurationMonths, decimal FeeAmount);

public record CourseLookupResult(bool Found, CourseSummary? Course, string? Message);

public class CourseTools
{
    private readonly ICourseService _courseService;

    public CourseTools(ICourseService courseService)
    {
        _courseService = courseService;
    }

    [Description("Get a single course's details by its exact course code, e.g. 'CS-401'." +
        " Always check the Found field first — if Found is false, no course with that code exists. " +
        "Do not substitute a different course.")]
    public CourseLookupResult GetCourseByCode(
        [Description("The course's code")] string code)
    {
        var course = _courseService.GetCourseByCode(code);
        return course == null
            ? new CourseLookupResult(false, null, $"No course exists with code '{code}'.")
            : new CourseLookupResult(true, ToSummary(course), null);
    }

    [Description(
    "Search for courses by full or partial course name. " +
    "Use this when the user provides a course name instead of " +
    "an exact course code or internal course ID. " +
    "Returns an empty list when no courses match.")]
    public IEnumerable<CourseSummary>
    SearchCoursesByName(
        [Description(
            "Full or partial course name to search for.")]
        string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return [];
        }

        return _courseService
            .GetAllCourses()
            .Where(course =>
                course.Name.Contains(
                    name,
                    StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .Select(ToSummary);
    }

    [Description("List all courses currently offered, including their fee and duration.")]
    public IEnumerable<CourseSummary> GetAllCourses()
    {
        return _courseService.GetAllCourses().Select(ToSummary);
    }

    [Description(
    "Find a course by its exact internal course ID. " +
    "Always check Found first. If Found is false, do not substitute another course.")]
    public CourseLookupResult GetCourseById(
    [Description("The exact internal course ID.")]
    int courseId)
    {
        var course = _courseService.GetCourseById(courseId);

        return course == null
            ? new CourseLookupResult(
                false,
                null,
                $"No course exists with ID '{courseId}'.")
            : new CourseLookupResult(
                true,
                ToSummary(course),
                null);
    }

    [Description(
    "Create a new course. " +
    "All required course information must come from the user. " +
    "Never invent a course code, name, duration, or fee amount. " +
    "This modifies course data and requires human approval.")]
    public string CreateCourse(
    [Description(
        "The unique course code.")]
    string code,

    [Description(
        "The course name.")]
    string name,

    [Description(
        "Course duration in months.")]
    int durationMonths,

    [Description(
        "Initial course fee amount.")]
    decimal feeAmount,

    [Description(
        "Optional course description.")]
    string? description = null)
    {
        var course =
            new Course(
                code,
                name,
                durationMonths,
                feeAmount)
            {
                Description = description
            };

        _courseService.CreateCourse(
            course);

        return
            $"Course '{course.Name}' " +
            $"with code '{course.Code}' " +
            $"was successfully created.";
    }

    [Description(
    "Update one or more details of an existing course. " +
    "Before using this tool, verify the exact course using GetCourseById. " +
    "Only explicitly supplied fields are changed. " +
    "This modifies course data and requires human approval.")]
    public string UpdateCourseDetails(
    [Description("The exact internal course ID.")]
    int courseId,

    [Description("New course name, or null to keep the current value.")]
    string? name = null,

    [Description("New course description, or null to keep the current value.")]
    string? description = null,

    [Description("New duration in months, or null to keep the current value.")]
    int? durationMonths = null)
    {
        var course = _courseService.GetCourseById(courseId);

        if (course is null)
        {
            return $"Course with ID {courseId} was not found. No changes were made.";
        }

        _courseService.UpdateCourseDetails(
            courseId,
            name ?? course.Name,
            description ?? course.Description,
            durationMonths ?? course.DurationMonths);

        return $"Course ID {courseId} was successfully updated.";
    }

    [Description(
    "Update the fee amount of an existing course. " +
    "Before using this tool, verify the exact course using GetCourseById. " +
    "This modifies course pricing and requires human approval.")]
    public string UpdateCoursePricing(
    [Description("The exact internal course ID.")]
    int courseId,

    [Description("The new course fee amount.")]
    decimal newFeeAmount)
    {
        var course = _courseService.GetCourseById(courseId);

        if (course is null)
        {
            return $"Course with ID {courseId} was not found. No pricing changes were made.";
        }

        _courseService.UpdateCoursePricing(
            courseId,
            newFeeAmount);

        return $"Course ID {courseId} fee was successfully updated to {newFeeAmount:C}.";
    }

    [Description(
    "Permanently remove an existing course from the system. " +
    "Before using this tool, verify the exact course using GetCourseById. " +
    "Never substitute another course if the requested course does not exist. " +
    "This is a destructive operation and requires human approval.")]
    public string RemoveCourse(
    [Description("The exact internal course ID.")]
    int courseId)
    {
        var course = _courseService.GetCourseById(courseId);

        if (course is null)
        {
            return $"Course with ID {courseId} was not found. No course was removed.";
        }

        _courseService.RemoveCourse(courseId);

        return $"Course ID {courseId} was successfully removed.";
    }

    private static CourseSummary ToSummary(Course course) =>
        new(course.Id, course.Code, course.Name, course.Description, course.DurationMonths, course.FeeAmount);
}