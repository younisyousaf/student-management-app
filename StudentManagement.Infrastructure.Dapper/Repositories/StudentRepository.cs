using System.Collections.Generic;
using System.Data;
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
            return db.QuerySingleOrDefault<Student>("usp_Student_GetById",
                new { Id = id }, commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<Student> GetAll()
        {
            using var db = CreateConnection();
            return db.Query<Student>("usp_Student_GetAll", commandType: CommandType.StoredProcedure);
        }

        public Student? GetByRollNumber(string rollNumber)
        {
            using var db = CreateConnection();
            return db.QuerySingleOrDefault<Student>("usp_Student_GetByRollNumber",
                new { RollNumber = rollNumber }, commandType: CommandType.StoredProcedure);
        }

        public Student? GetByEmail(string email)
        {
            using var db = CreateConnection();
            return db.QuerySingleOrDefault<Student>("usp_Student_GetByEmail",
                new { Email = email }, commandType: CommandType.StoredProcedure);
        }

        public void Add(Student entity)
        {
            using var db = CreateConnection();
            db.Execute("usp_Student_Insert", new
            {
                entity.RollNumber,
                entity.FirstName,
                entity.LastName,
                entity.Email,
                entity.Phone,
                entity.Address,
                entity.DateOfBirth,
                entity.AdmissionDate
            }, commandType: CommandType.StoredProcedure);
        }

        public void Update(Student entity)
        {
            using var db = CreateConnection();
            db.Execute("usp_Student_Update", new
            {
                entity.Id,
                entity.FirstName,
                entity.LastName,
                entity.Email,
                entity.Phone,
                entity.Address
            }, commandType: CommandType.StoredProcedure);
        }

        public void Delete(int id)
        {
            using var db = CreateConnection();
            db.Execute("usp_Student_Delete", new { Id = id }, commandType: CommandType.StoredProcedure);
        }
    }
}