using System.Collections.Generic;
using System.Data;
using System.Linq;
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
            string sql = "SELECT * FROM Courses WHERE Id = @Id";
            return db.QuerySingleOrDefault<Course>(sql, new { Id = id });
        }

        public IEnumerable<Course> GetAll()
        {
            using var db = CreateConnection();
            string sql = "SELECT * FROM Courses";
            return db.Query<Course>(sql);
        }

        public Course? GetByCode(string code)
        {
            using var db = CreateConnection();
            string sql = "SELECT * FROM Courses WHERE Code = @Code";
            return db.QuerySingleOrDefault<Course>(sql, new { Code = code });
        }

        public void Add(Course entity)
        {
            using var db = CreateConnection();
            string sql = @"
                INSERT INTO Courses (Code, Name, FeeAmount) 
                VALUES (@Code, @Name, @FeeAmount);";
            db.Execute(sql, entity);
        }

        public void Update(Course entity)
        {
            using var db = CreateConnection();
            string sql = @"
                UPDATE Courses 
                SET Code = @Code, Name = @Name, FeeAmount = @FeeAmount 
                WHERE Id = @Id;";
            db.Execute(sql, entity);
        }

        public void Delete(int id)
        {
            using var db = CreateConnection();
            string sql = "DELETE FROM Courses WHERE Id = @Id";
            db.Execute(sql, new { Id = id });
        }
    }
}