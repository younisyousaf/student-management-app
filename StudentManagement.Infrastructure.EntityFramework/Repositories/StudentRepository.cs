using System.Collections.Generic;
using System.Linq;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;

namespace StudentManagement.Infrastructure.EntityFramework.Repositories
{
    public class StudentRepository : EfRepository<Student>, IStudentRepository
    {
        public StudentRepository(AppDbContext context) : base(context) { }

        public Student? GetByRollNumber(string rollNumber)
        {
            return _context.Students.FirstOrDefault(s => s.RollNumber == rollNumber);
        }

        public Student? GetByEmail(string email)
        {
            return _context.Students.FirstOrDefault(s => s.Email == email);
        }
    }
}