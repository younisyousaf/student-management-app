using System.ComponentModel;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;

namespace StudentManagement.AI.Tools;

public record AttendanceRecord(int Id, int StudentId, int CourseId, DateTime Date, string Status, string? Remarks);

public record AttendanceLookupResult(bool Found, AttendanceRecord? Attendance, string? Message);

public class AttendanceTools
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceTools(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
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

    private static AttendanceRecord ToRecord(Attendance attendance) =>
        new(attendance.Id, attendance.StudentId, attendance.CourseId, attendance.Date, attendance.Status.ToString(), attendance.Remarks);
}