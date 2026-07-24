using System.Collections.Generic;
using System.Data;
using Dapper;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;

namespace StudentManagement.Infrastructure.Dapper.Repositories
{
    public class CourseRepository : DapperRepository, ICourseRepository
    {
        public CourseRepository(string connectionString) : base(connectionString) { }

        public Course? GetById(int id)
        {
            using var db = CreateConnection();
            return db.QuerySingleOrDefault<Course>("usp_Course_GetById", new { Id = id }, commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<Course> GetAll()
        {
            using var db = CreateConnection();
            return db.Query<Course>("usp_Course_GetAll", commandType: CommandType.StoredProcedure);
        }

        public Course? GetByCode(string code)
        {
            using var db = CreateConnection();
            return db.QuerySingleOrDefault<Course>("usp_Course_GetByCode", new { Code = code }, commandType: CommandType.StoredProcedure);
        }

        public void Add(Course entity)
        {
            using var db = CreateConnection();
            db.Execute("usp_Course_Insert", new
            {
                entity.Code,
                entity.Name,
                entity.Description,
                entity.DurationMonths,
                entity.FeeAmount
            }, commandType: CommandType.StoredProcedure);
        }

        public void Update(Course entity)
        {
            using var db = CreateConnection();
            db.Execute("usp_Course_Update", new
            {
                entity.Id,
                entity.Name,
                entity.Description,
                entity.DurationMonths,
                entity.FeeAmount
            }, commandType: CommandType.StoredProcedure);
        }

        public void Delete(int id)
        {
            using var db = CreateConnection();
            db.Execute("usp_Course_Delete", new { Id = id }, commandType: CommandType.StoredProcedure);
        }
    }
}