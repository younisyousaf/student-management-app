using StudentManagement.AI.Services;
using StudentManagement.Core.Enums;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;
using System.ComponentModel;

namespace StudentManagement.AI.Tools;

public record AttendanceRecord(int Id, int StudentId, int CourseId, DateTime Date, string Status, string? Remarks);
public record AttendanceLookupResult(bool Found, AttendanceRecord? Attendance, string? Message);
public record AttendanceSummaryResult(bool Found, AttendanceSummary? Summary, string? Message);

public class AttendanceTools
{
    private readonly IAttendanceService _attendanceService;
    private readonly IStudentService _studentService;
    private readonly IApplicationDateTime _applicationDateTime;

    public AttendanceTools(IAttendanceService attendanceService, IStudentService studentService, IApplicationDateTime applicationDateTime)
    {
        _attendanceService = attendanceService;
        _studentService = studentService;
        _applicationDateTime = applicationDateTime;
    }

    [Description("Get every attendance record for a specific student, by internal student ID, across all courses. " +
        "Returns an empty list if the student has no attendance records at all — that means none were recorded, not that the lookup failed. " +
        "This returns raw records only; it does not calculate an attendance percentage.")]
    public IEnumerable<AttendanceRecord> GetAttendanceForStudent(
        [Description("The student's internal numeric ID")] int studentId)
    {
        return _attendanceService.GetAttendanceForStudent(studentId).Select(ToRecord);
    }

    [Description("Get attendance records for every student in a specific course on a specific date. " +
        "Useful for 'who attended CS-101 on 2026-08-01' style questions.")]
    public IEnumerable<AttendanceRecord> GetAttendanceForCourseOnDate(
        [Description("The course's internal numeric ID")] int courseId,
        [Description("The date to check, e.g. 2026-08-01")] DateTime date)
    {
        return _attendanceService.GetAttendanceForCourseOnDate(courseId, date).Select(ToRecord);
    }

    [Description("Get a single attendance record by its ID. " +
        "Always check the Found field first — if Found is false, no attendance record with that ID exists.")]
    public AttendanceLookupResult GetAttendanceById(
        [Description("The attendance record's ID")] int attendanceId)
    {
        var attendance = _attendanceService.GetAttendanceById(attendanceId);
        return attendance == null
            ? new AttendanceLookupResult(false, null, $"No attendance record exists with ID {attendanceId}.")
            : new AttendanceLookupResult(true, ToRecord(attendance), null);
    }

    [Description("Get a student's overall attendance summary: total records, present/absent/late/excused counts, " +
        "and a calculated attendance percentage. Present and Late count toward the percentage; Absent and Excused do not, " +
        "though they are still included in the total record count. " +
        "Optionally scope to a single course via courseId, or omit it for the student's attendance across all enrolled courses. " +
        "Always check the Found field first — if Found is false, no student exists with that ID. " +
        "If Found is true but TotalRecords is 0, the student exists but has no attendance recorded yet — " +
        "do not describe this as a low or failing attendance percentage.")]
    public AttendanceSummaryResult GetAttendanceSummaryForStudent(
        [Description("The student's internal numeric ID")] int studentId,
        [Description("Optional: the course's internal numeric ID, to scope the summary to one course. Omit for all courses.")] int? courseId = null)
    {
        var student = _studentService.GetStudentById(studentId);
        if (student == null)
        {
            return new AttendanceSummaryResult(false, null, $"No student exists with ID {studentId}.");
        }

        var summary = _attendanceService.GetAttendanceSummary(studentId, courseId);
        return new AttendanceSummaryResult(true, summary, null);
    }

    [Description(
    "Marks attendance for a specific student in a specific course. " +
    "This modifies application data and must only be executed after human approval.")]
    public string MarkAttendance(
    [Description("The internal student ID.")] int studentId,
    [Description("The internal course ID.")] int courseId,
    [Description("The attendance date.")] DateTime date,
    [Description("The attendance status.")] AttendanceStatus status,
    [Description("Optional remarks for the attendance record.")] string? remarks = null)
    {
        _attendanceService.MarkAttendance(
            studentId,
            courseId,
            date,
            status,
            remarks);

        return $"Attendance successfully marked for student ID {studentId} in course ID {courseId}.";
    }

    [Description(
    "Mark today's attendance for a student in a course. " +
    "Use this tool when the user says today/current attendance. " +
    "This operation requires human approval.")]
    public string MarkAttendanceToday(
    [Description("The exact internal student ID.")]
    int studentId,

    [Description("The exact internal course ID.")]
    int courseId,

    [Description("Attendance status.")]
    AttendanceStatus status,

    [Description("Optional remarks.")]
    string? remarks = null)
    {
        DateTime date = _applicationDateTime.Today;

        _attendanceService.MarkAttendance(
            studentId,
            courseId,
            date,
            status,
            remarks);

        return $"Attendance marked as {status} for student ID {studentId} " +
               $"in course ID {courseId} for {date:yyyy-MM-dd}.";
    }

    [Description(
    "Update an existing attendance record's status and optional remarks. " +
    "This modifies application data and must only be executed after human approval. " +
    "Before using this tool, first use GetAttendanceById to verify that the exact attendance record exists.")]
    public string UpdateAttendance(
    [Description("The exact attendance record ID that was previously verified using GetAttendanceById.")]
    int attendanceId,

    [Description("The new attendance status.")]
    AttendanceStatus status,

    [Description("Optional updated remarks.")]
    string? remarks = null)
    {
        _attendanceService.UpdateAttendance(
            attendanceId,
            status,
            remarks);

        return $"Attendance record ID {attendanceId} was successfully updated to {status}.";
    }

    private static AttendanceRecord ToRecord(Attendance attendance) =>
        new(attendance.Id, attendance.StudentId, attendance.CourseId, attendance.Date, attendance.Status.ToString(), attendance.Remarks);
}