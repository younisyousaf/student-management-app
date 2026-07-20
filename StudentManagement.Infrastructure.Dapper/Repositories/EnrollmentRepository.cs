using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;

namespace StudentManagement.Infrastructure.Dapper.Repositories
{
    public class EnrollmentRepository : DapperRepository, IEnrollmentRepository
    {
        public EnrollmentRepository(string connectionString) : base(connectionString) { }

        public Enrollment? GetById(int id)
        {
            using var db = CreateConnection();
            string sql = "SELECT * FROM Enrollments WHERE Id = @Id";
            return db.QuerySingleOrDefault<Enrollment>(sql, new { Id = id });
        }

        public IEnumerable<Enrollment> GetAll()
        {
            using var db = CreateConnection();
            string sql = "SELECT * FROM Enrollments";
            return db.Query<Enrollment>(sql);
        }

        public bool IsAlreadyEnrolled(int studentId, int courseId)
        {
            using var db = CreateConnection();
            string sql = "SELECT COUNT(1) FROM Enrollments WHERE StudentId = @StudentId AND CourseId = @CourseId AND Status <> 'Dropped'";
            return db.ExecuteScalar<int>(sql, new { StudentId = studentId, CourseId = courseId }) > 0;
        }

        public IEnumerable<Enrollment> GetByStudentId(int studentId)
        {
            using var db = CreateConnection();
            string sql = "SELECT * FROM Enrollments WHERE StudentId = @StudentId";
            return db.Query<Enrollment>(sql, new { StudentId = studentId });
        }

        public void Add(Enrollment entity)
        {
            using var db = CreateConnection();
            string sql = @"
                INSERT INTO Enrollments (StudentId, CourseId, EnrollDate, Status) 
                VALUES (@StudentId, @CourseId, @EnrollDate, @Status);";
            db.Execute(sql, entity);
        }

        public void Update(Enrollment entity)
        {
            using var db = CreateConnection();
            string sql = @"
                UPDATE Enrollments 
                SET StudentId = @StudentId, CourseId = @CourseId, EnrollDate = @EnrollDate, Status = @Status 
                WHERE Id = @Id;";
            db.Execute(sql, entity);
        }

        public void Delete(int id)
        {
            using var db = CreateConnection();
            string sql = "DELETE FROM Enrollments WHERE Id = @Id";
            db.Execute(sql, new { Id = id });
        }
    }
}