using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;

namespace StudentManagement.Infrastructure.Hybrid.Repositories
{
    public class HybridEnrollmentRepository : IEnrollmentRepository
    {
        private readonly HybridDbContext _context;

        public HybridEnrollmentRepository(HybridDbContext context)
        {
            _context = context;
        }

        // READ OPERATIONS: Dapper
        public Enrollment? GetById(int id)
        {
            string sql = "SELECT * FROM Enrollments WHERE Id = @Id";
            return _context.Connection.QuerySingleOrDefault<Enrollment>(sql, new { Id = id });
        }

        public IEnumerable<Enrollment> GetAll()
        {
            string sql = "SELECT * FROM Enrollments";
            return _context.Connection.Query<Enrollment>(sql);
        }

        public bool IsAlreadyEnrolled(int studentId, int courseId)
        {
            string sql = "SELECT COUNT(1) FROM Enrollments WHERE StudentId = @StudentId AND CourseId = @CourseId AND Status <> 'Dropped'";
            return _context.Connection.ExecuteScalar<int>(sql, new { StudentId = studentId, CourseId = courseId }) > 0;
        }

        public IEnumerable<Enrollment> GetByStudentId(int studentId)
        {
            string sql = "SELECT * FROM Enrollments WHERE StudentId = @StudentId";
            return _context.Connection.Query<Enrollment>(sql, new { StudentId = studentId });
        }

        // WRITE OPERATIONS: EF Core
        public void Add(Enrollment entity)
        {
            _context.Enrollments.Add(entity);
            _context.SaveChanges();
        }

        public void Update(Enrollment entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var entity = _context.Enrollments.Find(id);
            if (entity != null)
            {
                _context.Enrollments.Remove(entity);
                _context.SaveChanges();
            }
        }
    }
}