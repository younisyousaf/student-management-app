using System.Collections.Generic;
using System.Linq;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;

namespace StudentManagement.Infrastructure.EntityFramework.Repositories
{
    public class EnrollmentRepository : EfRepository<Enrollment>, IEnrollmentRepository
    {
        public EnrollmentRepository(AppDbContext context) : base(context) { }

        public bool IsAlreadyEnrolled(int studentId, int courseId)
        {
            // Checks if an active or completed enrollment already exists for this pair
            return _context.Enrollments.Any(e => e.StudentId == studentId && e.CourseId == courseId && e.Status != "Dropped");
        }

        public IEnumerable<Enrollment> GetByStudentId(int studentId)
        {
            return _context.Enrollments.Where(e => e.StudentId == studentId).ToList();
        }
    }
}