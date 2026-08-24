using StudentManagement.AI.Models;
using StudentManagement.Core.Enums;
using StudentManagement.Core.Models;
using System.ComponentModel;

namespace StudentManagement.AI.Tools.Hosted;

public sealed class HostedAttendanceTools
{
    private readonly ScopedToolExecutor _executor;

    public HostedAttendanceTools(
        ScopedToolExecutor executor)
    {
        _executor = executor;
    }

    [Description(
        "Get every attendance record for a specific student.")]
    public IReadOnlyList<AttendanceRecord> GetAttendanceForStudent(
        [Description("The student's internal numeric ID.")]
        int studentId)
    {
        return _executor.Execute<
            AttendanceTools,
            IReadOnlyList<AttendanceRecord>>(
                tools =>
                    tools.GetAttendanceForStudent(studentId)
                        .ToList());
    }

    [Description(
        "Get attendance records for a course on a specific date.")]
    public IReadOnlyList<AttendanceRecord> GetAttendanceForCourseOnDate(
        [Description("The course's internal numeric ID.")]
        int courseId,

        [Description("The attendance date.")]
        DateTime date)
    {
        return _executor.Execute<
            AttendanceTools,
            IReadOnlyList<AttendanceRecord>>(
                tools =>
                    tools.GetAttendanceForCourseOnDate(
                            courseId,
                            date)
                        .ToList());
    }

    [Description(
        "Get a single attendance record by its exact record ID. " +
        "Always check Found first.")]
    public AttendanceLookupResult GetAttendanceById(
        [Description("The attendance record ID.")]
        int attendanceId)
    {
        return _executor.Execute<
            AttendanceTools,
            AttendanceLookupResult>(
                tools =>
                    tools.GetAttendanceById(
                        attendanceId));
    }

    [Description(
        "Get a student's calculated attendance summary. " +
        "Always check Success and Found before interpreting the data.")]
    public ToolResult<AttendanceSummary>
        GetAttendanceSummaryForStudent(
            [Description("The student's internal numeric ID.")]
            int studentId,

            [Description(
                "Optional course ID. Omit for all courses.")]
            int? courseId = null)
    {
        return _executor.Execute<
            AttendanceTools,
            ToolResult<AttendanceSummary>>(
                tools =>
                    tools.GetAttendanceSummaryForStudent(
                        studentId,
                        courseId));
    }

    [Description(
        "Mark attendance for a student on an explicitly supplied date. " +
        "This operation modifies application data and requires human approval.")]
    public string MarkAttendance(
        [Description("The exact internal student ID.")]
        int studentId,

        [Description("The exact internal course ID.")]
        int courseId,

        [Description("The exact attendance date.")]
        DateTime date,

        [Description("The attendance status.")]
        AttendanceStatus status,

        [Description("Optional attendance remarks.")]
        string? remarks = null)
    {
        return _executor.Execute<
            AttendanceTools,
            string>(
                tools =>
                    tools.MarkAttendance(
                        studentId,
                        courseId,
                        date,
                        status,
                        remarks));
    }

    [Description(
        "Mark today's attendance for a student in a course. " +
        "The application determines today's date from its configured timezone. " +
        "This operation requires human approval.")]
    public string MarkAttendanceToday(
        [Description("The exact internal student ID.")]
        int studentId,

        [Description("The exact internal course ID.")]
        int courseId,

        [Description("The attendance status.")]
        AttendanceStatus status,

        [Description("Optional remarks.")]
        string? remarks = null)
    {
        return _executor.Execute<
            AttendanceTools,
            string>(
                tools =>
                    tools.MarkAttendanceToday(
                        studentId,
                        courseId,
                        status,
                        remarks));
    }

    [Description(
        "Update an existing attendance record. " +
        "Verify the exact attendance record first. " +
        "This operation requires human approval.")]
    public string UpdateAttendance(
        [Description("The exact attendance record ID.")]
        int attendanceId,

        [Description("The new attendance status.")]
        AttendanceStatus status,

        [Description("Optional updated remarks.")]
        string? remarks = null)
    {
        return _executor.Execute<
            AttendanceTools,
            string>(
                tools =>
                    tools.UpdateAttendance(
                        attendanceId,
                        status,
                        remarks));
    }
}
