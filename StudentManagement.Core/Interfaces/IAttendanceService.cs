using StudentManagement.Core.Enums;
using StudentManagement.Core.Models;

namespace StudentManagement.Core.Interfaces;

public interface IAttendanceService
{
    void MarkAttendance(int studentId, int courseId, DateTime date, AttendanceStatus status, string? remarks = null);
    void UpdateAttendance(int attendanceId, AttendanceStatus status, string? remarks = null);
    IEnumerable<Attendance> GetAttendanceForStudent(int studentId);
    IEnumerable<Attendance> GetAttendanceForCourseOnDate(int courseId, DateTime date);
    IEnumerable<Attendance> GetAllAttendance();
    Attendance? GetAttendanceById(int attendanceId);
}