using StudentManagement.Core.Models;

namespace StudentManagement.Core.Interfaces;

public interface IStudentRepository : IRepository<Student>
{
    Student? GetByRollNumber(string rollNumber);
    Student? GetByEmail(string email);
}