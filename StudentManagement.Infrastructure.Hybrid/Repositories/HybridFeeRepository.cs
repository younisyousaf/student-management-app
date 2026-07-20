using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;

namespace StudentManagement.Infrastructure.Hybrid.Repositories
{
    public class HybridFeeRepository : IFeeRepository
    {
        private readonly HybridDbContext _context;

        public HybridFeeRepository(HybridDbContext context)
        {
            _context = context;
        }

        // READ OPERATIONS: Dapper
        public Fee? GetById(int id)
        {
            string sql = "SELECT * FROM Fees WHERE Id = @Id";
            return _context.Connection.QuerySingleOrDefault<Fee>(sql, new { Id = id });
        }

        public IEnumerable<Fee> GetAll()
        {
            string sql = "SELECT * FROM Fees";
            return _context.Connection.Query<Fee>(sql);
        }

        public Fee? GetByStudentAndCourse(int studentId, int courseId)
        {
            string sql = "SELECT * FROM Fees WHERE StudentId = @StudentId AND CourseId = @CourseId";
            return _context.Connection.QuerySingleOrDefault<Fee>(sql, new { StudentId = studentId, CourseId = courseId });
        }

        // WRITE OPERATIONS: EF Core
        public void Add(Fee entity)
        {
            _context.Fees.Add(entity);
            _context.SaveChanges();
        }

        public void Update(Fee entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var entity = _context.Fees.Find(id);
            if (entity != null)
            {
                _context.Fees.Remove(entity);
                _context.SaveChanges();
            }
        }
    }
}