using System.ComponentModel;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;

namespace StudentManagement.AI.Tools;

public record EnrollmentSummary(
    int Id,
    int StudentId,
    string StudentName,
    string RollNumber,
    int CourseId,
    string CourseName,
    string CourseCode,
    DateTime EnrollDate,
    string Status);

public record EnrollmentLookupResult(bool Found, EnrollmentSummary? Enrollment, string? Message);

public class EnrollmentTools
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly IStudentService _studentService;
    private readonly ICourseService _courseService;

    public EnrollmentTools(
        IEnrollmentService enrollmentService,
        IStudentService studentService,
        ICourseService courseService)
    {
        _enrollmentService =
            enrollmentService;

        _studentService =
            studentService;

        _courseService =
            courseService;
    }

    [Description("Get all enrollments (active, dropped, or completed) for a specific student, by the student's internal ID (not roll number). " +
        "Use get_student or search tools first if you only have a name or roll number. " +
        "Returns an empty list if the student has no enrollments — that means they are not enrolled in anything, not that the lookup failed.")]
    public IEnumerable<EnrollmentSummary> GetEnrollmentsByStudent(
        [Description("The student's internal numeric ID")] int studentId)
    {
        return _enrollmentService.GetEnrollmentsByStudent(studentId).Select(ToSummary);
    }

    [Description("Get a single enrollment record by its enrollment ID. " +
        "Always check the Found field first — if Found is false, no enrollment with that ID exists. " +
        "Do not substitute a different enrollment.")]
    public EnrollmentLookupResult GetEnrollmentById(
        [Description("The enrollment's ID")] int enrollmentId)
    {
        var enrollment = _enrollmentService.GetEnrollmentById(enrollmentId);
        return enrollment == null
            ? new EnrollmentLookupResult(false, null, $"No enrollment exists with ID {enrollmentId}.")
            : new EnrollmentLookupResult(true, ToSummary(enrollment), null);
    }

    [Description(
    "Check whether a student currently has an active enrollment " +
    "in a specific course. " +
    "Use this when both the exact student ID and course ID are known. " +
    "Always check Found before using the returned enrollment.")]
    public EnrollmentLookupResult
    GetEnrollmentForStudentCourse(
        [Description(
            "The exact internal student ID.")]
        int studentId,

        [Description(
            "The exact internal course ID.")]
        int courseId)
    {
        var enrollment =
            _enrollmentService
                .GetEnrollmentsByStudent(
                    studentId)
                .FirstOrDefault(
                    enrollment =>
                        enrollment.CourseId ==
                            courseId &&
                        string.Equals(
                            enrollment.Status,
                            "Active",
                            StringComparison
                                .OrdinalIgnoreCase));

        return enrollment is null
            ? new EnrollmentLookupResult(
                false,
                null,
                $"Student {studentId} does not have " +
                $"an active enrollment in course {courseId}.")
            : new EnrollmentLookupResult(
                true,
                ToSummary(enrollment),
                null);
    }

    [Description(
    "Get enrollment records for a specific course. " +
    "Returns active, completed, and dropped enrollments. " +
    "Use this when the user asks which students are or were " +
    "associated with a course.")]
    public IEnumerable<EnrollmentSummary>
    GetEnrollmentsByCourse(
        [Description(
            "The exact internal course ID.")]
        int courseId)
    {
        if (courseId <= 0)
        {
            return [];
        }

        return _enrollmentService
            .GetAllEnrollments()
            .Where(
                enrollment =>
                    enrollment.CourseId ==
                    courseId)
            .Take(50)
            .Select(ToSummary);
    }

    [Description(
    "Enroll a student in a course. " +
    "This changes application data and requires human approval before execution.")]
    public string EnrollStudent(
    [Description("The exact internal student ID.")] int studentId,
    [Description("The exact internal course ID.")] int courseId)
    {
        var student =
       _studentService.GetStudentById(
           studentId);

        var course =
            _courseService.GetCourseById(
                courseId);

        _enrollmentService.EnrollStudent(
            studentId,
            courseId);

        var studentName =
            student?.FullName ??
            "The student";

        var courseName =
            course?.Name ??
            "the course";

        return
            $"{studentName} was successfully enrolled " +
            $"in {courseName}.";
    }
    

    [Description(
    "Drop an existing enrollment. " +
    "Before using this tool, verify the exact enrollment using GetEnrollmentById. " +
    "This modifies application data and requires human approval.")]
    public string DropCourse(
    [Description("The exact enrollment record ID.")]
    int enrollmentId)
    {
        var enrollment =
        _enrollmentService.GetEnrollmentById(
            enrollmentId);

        if (enrollment is null)
        {
            return
                "The enrollment could not be found.";
        }

        var student =
            _studentService.GetStudentById(
                enrollment.StudentId);

        var course =
            _courseService.GetCourseById(
                enrollment.CourseId);

        _enrollmentService.DropCourse(
            enrollmentId);

        return
            $"{student?.FullName ?? "The student"} " +
            $"was successfully dropped from " +
            $"{course?.Name ?? "the course"}.";
    }

    [Description(
        "Mark an existing enrollment as completed. " +
        "Before using this tool, verify the exact enrollment using GetEnrollmentById. " +
        "This modifies application data and requires human approval.")]
    public string CompleteCourse(
        [Description("The exact enrollment record ID.")]
    int enrollmentId)
    {
        var enrollment =
        _enrollmentService.GetEnrollmentById(
            enrollmentId);

        if (enrollment is null)
        {
            return
                "The enrollment could not be found.";
        }

        var student =
            _studentService.GetStudentById(
                enrollment.StudentId);

        var course =
            _courseService.GetCourseById(
                enrollment.CourseId);

        _enrollmentService.CompleteCourse(
            enrollmentId);

        return
            $"{student?.FullName ?? "The student"} " +
            $"successfully completed " +
            $"{course?.Name ?? "the course"}.";
    }

    private EnrollmentSummary ToSummary(Enrollment enrollment)
    {
        var student =
            _studentService.GetStudentById(
                enrollment.StudentId);

        var course =
            _courseService.GetCourseById(
                enrollment.CourseId);

        return new EnrollmentSummary(
            Id:
                enrollment.Id,

            StudentId:
                enrollment.StudentId,

            StudentName:
                student?.FullName ??
                "Unknown student",

            RollNumber:
                student?.RollNumber ??
                string.Empty,

            CourseId:
                enrollment.CourseId,

            CourseName:
                course?.Name ??
                "Unknown course",

            CourseCode:
                course?.Code ??
                string.Empty,

            EnrollDate:
                enrollment.EnrollDate,

            Status:
                enrollment.Status);
    }
}
