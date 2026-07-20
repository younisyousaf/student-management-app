using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;

namespace StudentManagement.Infrastructure.Hybrid.Repositories
{
    public class HybridStudentRepository : IStudentRepository
    {
        private readonly HybridDbContext _context;

        public HybridStudentRepository(HybridDbContext context)
        {
            _context = context;
        }

        // READ OPERATIONS: Dapper
        public Student? GetById(int id)
        {
            string sql = "SELECT * FROM Students WHERE Id = @Id";
            return _context.Connection.QuerySingleOrDefault<Student>(sql, new { Id = id });
        }

        public IEnumerable<Student> GetAll()
        {
            string sql = "SELECT * FROM Students";
            return _context.Connection.Query<Student>(sql);
        }

        public Student? GetByRollNumber(string rollNumber)
        {
            string sql = "SELECT * FROM Students WHERE RollNumber = @RollNumber";
            return _context.Connection.QuerySingleOrDefault<Student>(sql, new { RollNumber = rollNumber });
        }

        public Student? GetByEmail(string email)
        {
            string sql = "SELECT * FROM Students WHERE Email = @Email";
            return _context.Connection.QuerySingleOrDefault<Student>(sql, new { Email = email });
        }

        // WRITE OPERATIONS: EF Core
        public void Add(Student entity)
        {
            _context.Students.Add(entity);
            _context.SaveChanges();
        }

        public void Update(Student entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var entity = _context.Students.Find(id);
            if (entity != null)
            {
                _context.Students.Remove(entity);
                _context.SaveChanges();
            }
        }
    }
}