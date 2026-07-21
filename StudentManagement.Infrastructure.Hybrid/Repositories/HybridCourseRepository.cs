using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;

namespace StudentManagement.Infrastructure.Hybrid.Repositories
{
	public class HybridCourseRepository : ICourseRepository
	{
		private readonly HybridDbContext _context;

		public HybridCourseRepository(HybridDbContext context)
		{
			_context = context;
		}

		// READ OPERATIONS: Dapper
		public Course? GetById(int id)
		{
			string sql = "SELECT * FROM Courses WHERE Id = @Id";
			return _context.Connection.QuerySingleOrDefault<Course>(sql, new { Id = id });
		}

		public IEnumerable<Course> GetAll()
		{
			string sql = "SELECT * FROM Courses";
			return _context.Connection.Query<Course>(sql);
		}

		public Course? GetByCode(string code)
		{
			string sql = "SELECT * FROM Courses WHERE Code = @Code";
			return _context.Connection.QuerySingleOrDefault<Course>(sql, new { Code = code });
		}

		// WRITE OPERATIONS: EF Core
		public void Add(Course entity)
		{
			_context.Courses.Add(entity);
			_context.SaveChanges();
		}

		public void Update(Course entity)
		{
			_context.Entry(entity).State = EntityState.Modified;
			_context.SaveChanges();
            _context.Entry(entity).State = EntityState.Detached;
        }

		public void Delete(int id)
		{
			var entity = _context.Courses.Find(id);
			if (entity != null)
			{
				_context.Courses.Remove(entity);
				_context.SaveChanges();
			}
		}
	}
}