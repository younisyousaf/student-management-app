using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;

namespace StudentManagement.Infrastructure.Dapper.Repositories
{
    public class AttendanceRepository : DapperRepository, IAttendanceRepository
    {
        public AttendanceRepository(string connectionString) : base(connectionString) { }

        public Attendance? GetById(int id)
        {
            using var db = CreateConnection();
            return db.QuerySingleOrDefault<Attendance>("usp_Attendance_GetById", new { Id = id }, commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<Attendance> GetAll()
        {
            using var db = CreateConnection();
            return db.Query<Attendance>("usp_Attendance_GetAll", commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<Attendance> GetByStudentId(int studentId)
        {
            using var db = CreateConnection();
            return db.Query<Attendance>("usp_Attendance_GetByStudentId", new { StudentId = studentId }, commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<Attendance> GetByCourseAndDate(int courseId, DateTime date)
        {
            using var db = CreateConnection();
            return db.Query<Attendance>("usp_Attendance_GetByCourseAndDate",
                new { CourseId = courseId, Date = date.Date }, commandType: CommandType.StoredProcedure);
        }

        public Attendance? GetByStudentCourseAndDate(int studentId, int courseId, DateTime date)
        {
            using var db = CreateConnection();
            return db.QuerySingleOrDefault<Attendance>("usp_Attendance_GetByStudentCourseAndDate",
                new { StudentId = studentId, CourseId = courseId, Date = date.Date }, commandType: CommandType.StoredProcedure);
        }

        public void Add(Attendance entity)
        {
            using var db = CreateConnection();
            db.Execute("usp_Attendance_Insert", new
            {
                entity.StudentId,
                entity.CourseId,
                entity.Date,
                Status = (int)entity.Status,
                entity.Remarks
            }, commandType: CommandType.StoredProcedure);
        }

        public void Update(Attendance entity)
        {
            using var db = CreateConnection();
            db.Execute("usp_Attendance_Update", new
            {
                entity.Id,
                Status = (int)entity.Status,
                entity.Remarks
            }, commandType: CommandType.StoredProcedure);
        }

        public void Delete(int id)
        {
            using var db = CreateConnection();
            db.Execute("usp_Attendance_Delete", new { Id = id }, commandType: CommandType.StoredProcedure);
        }
    }
}