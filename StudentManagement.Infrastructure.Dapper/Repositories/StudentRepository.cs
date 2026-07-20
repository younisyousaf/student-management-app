using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;

namespace StudentManagement.Infrastructure.Dapper.Repositories
{
    public class StudentRepository : DapperRepository, IStudentRepository
    {
        public StudentRepository(string connectionString) : base(connectionString) { }

        public Student? GetById(int id)
        {
            using var db = CreateConnection();
            string sql = "SELECT * FROM Students WHERE Id = @Id";
            return db.QuerySingleOrDefault<Student>(sql, new { Id = id });
        }

        public IEnumerable<Student> GetAll()
        {
            using var db = CreateConnection();
            string sql = "SELECT * FROM Students";
            return db.Query<Student>(sql);
        }

        public Student? GetByRollNumber(string rollNumber)
        {
            using var db = CreateConnection();
            string sql = "SELECT * FROM Students WHERE RollNumber = @RollNumber";
            return db.QuerySingleOrDefault<Student>(sql, new { RollNumber = rollNumber });
        }

        public Student? GetByEmail(string email)
        {
            using var db = CreateConnection();
            string sql = "SELECT * FROM Students WHERE Email = @Email";
            return db.QuerySingleOrDefault<Student>(sql, new { Email = email });
        }

        public void Add(Student entity)
        {
            using var db = CreateConnection();
            string sql = @"
                INSERT INTO Students (RollNumber, FirstName, LastName, Email) 
                VALUES (@RollNumber, @FirstName, @LastName, @Email);";
            db.Execute(sql, entity);
        }

        public void Update(Student entity)
        {
            using var db = CreateConnection();
            string sql = @"
                UPDATE Students 
                SET RollNumber = @RollNumber, FirstName = @FirstName, LastName = @LastName, Email = @Email 
                WHERE Id = @Id;";
            db.Execute(sql, entity);
        }

        public void Delete(int id)
        {
            using var db = CreateConnection();
            string sql = "DELETE FROM Students WHERE Id = @Id";
            db.Execute(sql, new { Id = id });
        }
    }
}