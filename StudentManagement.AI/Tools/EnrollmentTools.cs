using System.ComponentModel;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;

namespace StudentManagement.AI.Tools;

public record EnrollmentSummary(int Id, int StudentId, int CourseId, DateTime EnrollDate, string Status);

public record EnrollmentLookupResult(bool Found, EnrollmentSummary? Enrollment, string? Message);

public class EnrollmentTools
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentTools(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
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

    private static EnrollmentSummary ToSummary(Enrollment enrollment) =>
        new(enrollment.Id, enrollment.StudentId, enrollment.CourseId, enrollment.EnrollDate, enrollment.Status);
}