using System.Linq;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;

namespace StudentManagement.Infrastructure.EntityFramework.Repositories
{
    public class FeeRepository : EfRepository<Fee>, IFeeRepository
    {
        public FeeRepository(AppDbContext context) : base(context) { }

        public Fee? GetByStudentAndCourse(int studentId, int courseId)
        {
            return _context.Fees.FirstOrDefault(f => f.StudentId == studentId && f.CourseId == courseId);
        }
    }
}