using System.Linq;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;

namespace StudentManagement.Infrastructure.EntityFramework.Repositories
{
    public class CourseRepository : EfRepository<Course>, ICourseRepository
    {
        public CourseRepository(AppDbContext context) : base(context) { }

        public Course? GetByCode(string code)
        {
            return _context.Courses.FirstOrDefault(c => c.Code == code);
        }
    }
}