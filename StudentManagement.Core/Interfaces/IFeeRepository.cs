using StudentManagement.Core.Models;

namespace StudentManagement.Core.Interfaces;

public interface IFeeRepository : IRepository<Fee>
{
    Fee? GetByStudentAndCourse(int studentId, int courseId);
}