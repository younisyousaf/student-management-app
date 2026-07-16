using StudentManagement.Core.Models;

namespace StudentManagement.Core.Interfaces;

public interface ICourseRepository : IRepository<Course>
{
    Course? GetByCode(string code);
}