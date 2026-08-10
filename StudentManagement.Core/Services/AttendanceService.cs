using StudentManagement.Core.Enums;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;

namespace StudentManagement.Core.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;

    public AttendanceService(IAttendanceRepository attendanceRepository, IEnrollmentRepository enrollmentRepository)
    {
        _attendanceRepository = attendanceRepository;
        _enrollmentRepository = enrollmentRepository;
    }

    public void MarkAttendance(int studentId, int courseId, DateTime date, AttendanceStatus status, string? remarks = null)
    {
        if (!_enrollmentRepository.IsAlreadyEnrolled(studentId, courseId))
            throw new InvalidOperationException("Student is not actively enrolled in this course.");

        if (_attendanceRepository.GetByStudentCourseAndDate(studentId, courseId, date) != null)
            throw new InvalidOperationException("Attendance for this student, course, and date is already recorded. Use update instead.");

        _attendanceRepository.Add(new Attendance(studentId, courseId, date, status, remarks));
    }

    public void UpdateAttendance(int attendanceId, AttendanceStatus status, string? remarks = null)
    {
        var attendance = _attendanceRepository.GetById(attendanceId)
            ?? throw new KeyNotFoundException("Attendance record not found.");

        attendance.UpdateStatus(status, remarks);
        _attendanceRepository.Update(attendance);
    }

    public AttendanceSummary GetAttendanceSummary(int studentId, int? courseId = null)
    {
        var records = _attendanceRepository.GetByStudentId(studentId);

        if (courseId.HasValue)
            records = records.Where(a => a.CourseId == courseId.Value);

        var recordsList = records.ToList();
        var total = recordsList.Count;

        var present = recordsList.Count(a => a.Status == AttendanceStatus.Present);
        var absent = recordsList.Count(a => a.Status == AttendanceStatus.Absent);
        var late = recordsList.Count(a => a.Status == AttendanceStatus.Late);
        var excused = recordsList.Count(a => a.Status == AttendanceStatus.Excused);

        // Present and Late = 1
        // Absent and Excused = 0
        var countedPresent = present + late;
        var percentage = total == 0 ? 0.0 : Math.Round((double)countedPresent / total * 100, 2);

        return new AttendanceSummary(studentId, courseId, total, present, absent, late, excused, percentage);
    }

    public IEnumerable<Attendance> GetAllAttendance() => _attendanceRepository.GetAll();

    public IEnumerable<Attendance> GetAttendanceForStudent(int studentId) =>
        _attendanceRepository.GetByStudentId(studentId);

    public IEnumerable<Attendance> GetAttendanceForCourseOnDate(int courseId, DateTime date) =>
        _attendanceRepository.GetByCourseAndDate(courseId, date);
    public Attendance? GetAttendanceById(int attendanceId) => _attendanceRepository.GetById(attendanceId);
}