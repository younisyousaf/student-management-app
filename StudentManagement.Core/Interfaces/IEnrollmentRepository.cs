using StudentManagement.Core.Models;

namespace StudentManagement.Core.Interfaces;

public interface IEnrollmentRepository : IRepository<Enrollment>
{

    bool IsAlreadyEnrolled(int studentId, int courseId);

    IEnumerable<Enrollment> GetByStudentId(int studentId);
}