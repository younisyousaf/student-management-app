using System.Collections.Generic;
using System.Data;
using System.Linq;
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
            string sql = "SELECT * FROM Fees WHERE Id = @Id";
            return db.QuerySingleOrDefault<Fee>(sql, new { Id = id });
        }

        public IEnumerable<Fee> GetAll()
        {
            using var db = CreateConnection();
            string sql = "SELECT * FROM Fees";
            return db.Query<Fee>(sql);
        }

        public Fee? GetByStudentAndCourse(int studentId, int courseId)
        {
            using var db = CreateConnection();
            string sql = "SELECT * FROM Fees WHERE StudentId = @StudentId AND CourseId = @CourseId";
            return db.QuerySingleOrDefault<Fee>(sql, new { StudentId = studentId, CourseId = courseId });
        }

        public void Add(Fee entity)
        {
            using var db = CreateConnection();
            string sql = @"
                INSERT INTO Fees (StudentId, CourseId, AmountDue, AmountPaid, PaymentDate, Remarks) 
                VALUES (@StudentId, @CourseId, @AmountDue, @AmountPaid, @PaymentDate, @Remarks);";
            db.Execute(sql, entity);
        }

        public void Update(Fee entity)
        {
            using var db = CreateConnection();
            string sql = @"
                UPDATE Fees 
                SET StudentId = @StudentId, CourseId = @CourseId, AmountDue = @AmountDue, 
                    AmountPaid = @AmountPaid, PaymentDate = @PaymentDate, Remarks = @Remarks 
                WHERE Id = @Id;";
            db.Execute(sql, entity);
        }

        public void Delete(int id)
        {
            using var db = CreateConnection();
            string sql = "DELETE FROM Fees WHERE Id = @Id";
            db.Execute(sql, new { Id = id });
        }
    }
}