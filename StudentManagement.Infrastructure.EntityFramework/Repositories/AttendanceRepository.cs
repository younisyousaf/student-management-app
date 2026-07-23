using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StudentManagement.Infrastructure.EntityFramework.Repositories
{
    public class AttendanceRepository : EfRepository<Attendance>, IAttendanceRepository
    {
        public AttendanceRepository(AppDbContext context) : base(context) { }

        public IEnumerable<Attendance> GetByStudentId(int studentId)
        {
            return _context.Attendances
                .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.Date)
                .AsNoTracking()
                .ToList();
        }

        public IEnumerable<Attendance> GetByCourseAndDate(int courseId, DateTime date)
        {
            return _context.Attendances
                .Where(a => a.CourseId == courseId && a.Date == date.Date)
                .AsNoTracking()
                .ToList();
        }

        public Attendance? GetByStudentCourseAndDate(int studentId, int courseId, DateTime date)
        {
            return _context.Attendances
                .FirstOrDefault(a => a.StudentId == studentId && a.CourseId == courseId && a.Date == date.Date);
        }
    }
}