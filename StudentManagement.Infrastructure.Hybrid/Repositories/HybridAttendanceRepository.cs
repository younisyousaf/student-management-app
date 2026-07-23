using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;

namespace StudentManagement.Infrastructure.Hybrid.Repositories
{
    public class HybridAttendanceRepository : IAttendanceRepository
    {
        private readonly HybridDbContext _context;

        public HybridAttendanceRepository(HybridDbContext context)
        {
            _context = context;
        }

        // READ OPERATIONS: Dapper
        public Attendance? GetById(int id)
        {
            string sql = "SELECT * FROM Attendances WHERE Id = @Id";
            return _context.Connection.QuerySingleOrDefault<Attendance>(sql, new { Id = id });
        }

        public IEnumerable<Attendance> GetAll()
        {
            string sql = "SELECT * FROM Attendances";
            return _context.Connection.Query<Attendance>(sql);
        }

        public IEnumerable<Attendance> GetByStudentId(int studentId)
        {
            string sql = "SELECT * FROM Attendances WHERE StudentId = @StudentId ORDER BY Date DESC";
            return _context.Connection.Query<Attendance>(sql, new { StudentId = studentId });
        }

        public IEnumerable<Attendance> GetByCourseAndDate(int courseId, DateTime date)
        {
            string sql = "SELECT * FROM Attendances WHERE CourseId = @CourseId AND Date = @Date";
            return _context.Connection.Query<Attendance>(sql, new { CourseId = courseId, Date = date.Date });
        }

        public Attendance? GetByStudentCourseAndDate(int studentId, int courseId, DateTime date)
        {
            string sql = "SELECT * FROM Attendances WHERE StudentId = @StudentId AND CourseId = @CourseId AND Date = @Date";
            return _context.Connection.QuerySingleOrDefault<Attendance>(sql, new { StudentId = studentId, CourseId = courseId, Date = date.Date });
        }

        // WRITE OPERATIONS: EF Core
        public void Add(Attendance entity)
        {
            _context.Attendances.Add(entity);
            _context.SaveChanges();
        }

        public void Update(Attendance entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            _context.SaveChanges();
            _context.Entry(entity).State = EntityState.Detached;
        }

        public void Delete(int id)
        {
            var entity = _context.Attendances.Find(id);
            if (entity != null)
            {
                _context.Attendances.Remove(entity);
                _context.SaveChanges();
            }
        }
    }
}