using StudentManagement.Core.Models;

namespace StudentManagement.Core.Interfaces;

public interface IAttendanceRepository : IRepository<Attendance>
{
    IEnumerable<Attendance> GetByStudentId(int studentId);
    IEnumerable<Attendance> GetByCourseAndDate(int courseId, DateTime date);
    Attendance? GetByStudentCourseAndDate(int studentId, int courseId, DateTime date);
}