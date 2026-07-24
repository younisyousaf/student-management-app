using System.Collections.Generic;
using System.Data;
using Dapper;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;

namespace StudentManagement.Infrastructure.Dapper.Repositories
{
    public class FeeRepository : DapperRepository, IFeeRepository
    {
        public FeeRepository(string connectionString) : base(connectionString) { }

        public Fee? GetById(int id)
        {
            using var db = CreateConnection();
            return db.QuerySingleOrDefault<Fee>("usp_Fee_GetById", new { Id = id }, commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<Fee> GetAll()
        {
            using var db = CreateConnection();
            return db.Query<Fee>("usp_Fee_GetAll", commandType: CommandType.StoredProcedure);
        }

        public Fee? GetByStudentAndCourse(int studentId, int courseId)
        {
            using var db = CreateConnection();
            return db.QuerySingleOrDefault<Fee>("usp_Fee_GetByStudentAndCourse",
                new { StudentId = studentId, CourseId = courseId }, commandType: CommandType.StoredProcedure);
        }

        public void Add(Fee entity)
        {
            using var db = CreateConnection();
            db.Execute("usp_Fee_Insert", new { entity.StudentId, entity.CourseId, entity.AmountDue }, commandType: CommandType.StoredProcedure);
        }

        public void Update(Fee entity)
        {
            using var db = CreateConnection();
            db.Execute("usp_Fee_Update", new
            {
                entity.Id,
                entity.AmountPaid,
                entity.PaymentDate,
                entity.Remarks
            }, commandType: CommandType.StoredProcedure);
        }

        public void Delete(int id)
        {
            using var db = CreateConnection();
            // Intentionally left unimplemented — every other app in this system blocks deleting
            // financial records at the controller/service level, never at the repository.
            // A stored procedure exists here on purpose: usp_Fee_Delete was NOT created above,
            // matching that same "this action should not be possible" decision at the data layer too.
            throw new System.NotSupportedException("Fee records cannot be deleted.");
        }
    }
}