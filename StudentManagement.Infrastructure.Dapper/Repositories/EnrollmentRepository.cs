using System.Collections.Generic;
using System.Data;
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
            return db.QuerySingleOrDefault<Enrollment>("usp_Enrollment_GetById", new { Id = id }, commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<Enrollment> GetAll()
        {
            using var db = CreateConnection();
            return db.Query<Enrollment>("usp_Enrollment_GetAll", commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<Enrollment> GetByStudentId(int studentId)
        {
            using var db = CreateConnection();
            return db.Query<Enrollment>("usp_Enrollment_GetByStudentId", new { StudentId = studentId }, commandType: CommandType.StoredProcedure);
        }

        public bool IsAlreadyEnrolled(int studentId, int courseId)
        {
            using var db = CreateConnection();
            return db.QuerySingle<bool>("usp_Enrollment_IsAlreadyEnrolled",
                new { StudentId = studentId, CourseId = courseId }, commandType: CommandType.StoredProcedure);
        }

        public void Add(Enrollment entity)
        {
            using var db = CreateConnection();
            db.Execute("usp_Enrollment_Insert", new
            {
                entity.StudentId,
                entity.CourseId,
                entity.EnrollDate,
                entity.Status
            }, commandType: CommandType.StoredProcedure);
        }

        public void Update(Enrollment entity)
        {
            using var db = CreateConnection();
            db.Execute("usp_Enrollment_UpdateStatus", new { entity.Id, entity.Status }, commandType: CommandType.StoredProcedure);
        }

        public void Delete(int id)
        {
            using var db = CreateConnection();
            db.Execute("usp_Enrollment_Delete", new { Id = id }, commandType: CommandType.StoredProcedure);
        }
    }
}