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

    [Description("List all courses currently offered, including their fee and duration.")]
    public IEnumerable<CourseSummary> GetAllCourses()
    {
        return _courseService.GetAllCourses().Select(ToSummary);
    }

    private static CourseSummary ToSummary(Course course) =>
        new(course.Id, course.Code, course.Name, course.Description, course.DurationMonths, course.FeeAmount);
}