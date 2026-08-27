using StudentManagement.AI.Models;
using StudentManagement.AI.Services;
using StudentManagement.Core.Enums;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;
using System.ComponentModel;
using StudentManagement.Core.Exceptions;

namespace StudentManagement.AI.Tools;

public record AttendanceRecord(
    int Id,
    int StudentId,
    string StudentName,
    string RollNumber,
    int CourseId,
    string CourseName,
    string CourseCode,
    DateTime Date,
    string Status,
    string? Remarks);

public record AttendanceLookupResult(
    bool Found,
    AttendanceRecord? Attendance,
    string? Message);

public record AttendanceSummaryResult(
    bool Found,
    AttendanceSummary? Summary,
    string? Message);

public record AttendanceSummaryView(
    int StudentId,
    string StudentName,
    string RollNumber,
    int? CourseId,
    string? CourseName,
    string? CourseCode,
    int TotalRecords,
    int PresentCount,
    int AbsentCount,
    int LateCount,
    int ExcusedCount,
    double AttendancePercentage);


public class AttendanceTools
{
    private readonly IAttendanceService _attendanceService;
    private readonly IStudentService _studentService;
    private readonly ICourseService _courseService;
    private readonly IApplicationDateTime _applicationDateTime;

    public AttendanceTools(
    IAttendanceService attendanceService,
    IStudentService studentService,
    ICourseService courseService,
    IApplicationDateTime applicationDateTime)
    {
        _attendanceService = attendanceService;
        _studentService = studentService;
        _courseService = courseService;
        _applicationDateTime = applicationDateTime;
    }

    [Description(
        "Get every attendance record for a specific student, by internal student ID, across all courses. " +
        "Returns an empty list if the student has no attendance records at all — that means none were recorded, not that the lookup failed. " +
        "This returns raw records only; it does not calculate an attendance percentage.")]
    public IEnumerable<AttendanceRecord> GetAttendanceForStudent(
        [Description("The student's internal numeric ID")]
        int studentId)
    {
        return _attendanceService
            .GetAttendanceForStudent(studentId)
            .Select(ToRecord);
    }

    [Description(
        "Get attendance records for every student in a specific course on a specific date. " +
        "Useful for 'who attended CS-101 on 2026-08-01' style questions.")]
    public IEnumerable<AttendanceRecord> GetAttendanceForCourseOnDate(
        [Description("The course's internal numeric ID")]
        int courseId,

        [Description("The date to check, e.g. 2026-08-01")]
        DateTime date)
    {
        return _attendanceService
            .GetAttendanceForCourseOnDate(courseId, date)
            .Select(ToRecord);
    }

    [Description(
        "Get a single attendance record by its ID. " +
        "Always check the Found field first — if Found is false, no attendance record with that ID exists.")]
    public AttendanceLookupResult GetAttendanceById(
        [Description("The attendance record's ID")]
        int attendanceId)
    {
        var attendance =
            _attendanceService.GetAttendanceById(attendanceId);

        return attendance == null
            ? new AttendanceLookupResult(
                false,
                null,
                $"No attendance record exists with ID {attendanceId}.")
            : new AttendanceLookupResult(
                true,
                ToRecord(attendance),
                null);
    }

    [Description(
        "Get a student's overall attendance summary: total records, present/absent/late/excused counts, " +
        "and a calculated attendance percentage. Present and Late count toward the percentage; Absent and Excused do not. " +
        "Optionally scope to a single course via courseId, or omit it for all courses. " +
        "Always check Success first. If Success is false, the attendance data could not be retrieved. " +
        "If Success is true, check Found. If Found is false, no student exists with that ID. " +
        "If Success and Found are true but TotalRecords is 0, the student exists but has no attendance records.")]
    public ToolResult<AttendanceSummaryView>
    GetAttendanceSummaryForStudent(
        [Description(
            "The student's internal numeric ID.")]
        int studentId,

        [Description(
            "Optional: the course's internal numeric ID, to scope the summary to one course. " +
            "Omit for all courses.")]
        int? courseId = null)
    {
        try
        {
            var student =
                _studentService.GetStudentById(
                    studentId);

            if (student is null)
            {
                return new ToolResult<AttendanceSummaryView>(
                    Success: true,
                    Found: false,
                    Data: null,
                    Message:
                        "The requested student was not found.");
            }

            var summary =
                _attendanceService
                    .GetAttendanceSummary(
                        studentId,
                        courseId);

            var course =
                courseId.HasValue
                    ? _courseService.GetCourseById(
                        courseId.Value)
                    : null;

            var view =
                new AttendanceSummaryView(
                    StudentId:
                        summary.StudentId,

                    StudentName:
                        student.FullName,

                    RollNumber:
                        student.RollNumber,

                    CourseId:
                        summary.CourseId,

                    CourseName:
                        course?.Name,

                    CourseCode:
                        course?.Code,

                    TotalRecords:
                        summary.TotalRecords,

                    PresentCount:
                        summary.PresentCount,

                    AbsentCount:
                        summary.AbsentCount,

                    LateCount:
                        summary.LateCount,

                    ExcusedCount:
                        summary.ExcusedCount,

                    AttendancePercentage:
                        summary.AttendancePercentage);

            return new ToolResult<AttendanceSummaryView>(
                Success: true,
                Found: true,
                Data: view,
                Message: null);
        }
        catch (ApplicationDataUnavailableException)
        {
            return new ToolResult<AttendanceSummaryView>(
                Success: false,
                Found: false,
                Data: null,
                Message:
                    "Attendance data is temporarily unavailable.");
        }
    }

    [Description(
        "Marks attendance for a specific student in a specific course. " +
        "This modifies application data and must only be executed after human approval.")]
    public string MarkAttendance(
        [Description("The internal student ID.")]
        int studentId,

        [Description("The internal course ID.")]
        int courseId,

        [Description("The attendance date.")]
        DateTime date,

        [Description("The attendance status.")]
        AttendanceStatus status,

        [Description("Optional remarks for the attendance record.")]
        string? remarks = null)
    {
        var student =
       _studentService.GetStudentById(
           studentId);

        var course =
            _courseService.GetCourseById(
                courseId);

        _attendanceService.MarkAttendance(
            studentId,
            courseId,
            date,
            status,
            remarks);

        return
            $"Attendance for {student?.FullName ?? "the student"} " +
            $"in {course?.Name ?? "the course"} " +
            $"was marked as {status} for {date:yyyy-MM-dd}.";
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
        DateTime date =
            _applicationDateTime.Today;

        var student =
            _studentService.GetStudentById(
                studentId);

        var course =
            _courseService.GetCourseById(
                courseId);

        _attendanceService.MarkAttendance(
            studentId,
            courseId,
            date,
            status,
            remarks);

        return
            $"Today's attendance for " +
            $"{student?.FullName ?? "the student"} " +
            $"in {course?.Name ?? "the course"} " +
            $"was marked as {status}.";
    }

    [Description(
        "Update an existing attendance record's status and optional remarks. " +
        "This modifies application data and must only be executed after human approval. " +
        "Before using this tool, first use GetAttendanceById to verify that the exact attendance record exists.")]
    public string UpdateAttendance(
        [Description(
            "The exact attendance record ID that was previously verified using GetAttendanceById.")]
        int attendanceId,

        [Description("The new attendance status.")]
        AttendanceStatus status,

        [Description("Optional updated remarks.")]
        string? remarks = null)
    {
        var attendance =
        _attendanceService.GetAttendanceById(
            attendanceId);

        if (attendance is null)
        {
            return
                "The attendance record could not be found.";
        }

        var student =
            _studentService.GetStudentById(
                attendance.StudentId);

        var course =
            _courseService.GetCourseById(
                attendance.CourseId);

        _attendanceService.UpdateAttendance(
            attendanceId,
            status,
            remarks);

        return
            $"Attendance for {student?.FullName ?? "the student"} " +
            $"in {course?.Name ?? "the course"} " +
            $"on {attendance.Date:yyyy-MM-dd} " +
            $"was updated to {status}.";
    }

    private AttendanceRecord ToRecord(
    Attendance attendance)
    {
        var student =
            _studentService.GetStudentById(
                attendance.StudentId);

        var course =
            _courseService.GetCourseById(
                attendance.CourseId);

        return new AttendanceRecord(
            Id:
                attendance.Id,

            StudentId:
                attendance.StudentId,

            StudentName:
                student?.FullName ??
                "Unknown student",

            RollNumber:
                student?.RollNumber ??
                string.Empty,

            CourseId:
                attendance.CourseId,

            CourseName:
                course?.Name ??
                "Unknown course",

            CourseCode:
                course?.Code ??
                string.Empty,

            Date:
                attendance.Date,

            Status:
                attendance.Status.ToString(),

            Remarks:
                attendance.Remarks);
    }
}