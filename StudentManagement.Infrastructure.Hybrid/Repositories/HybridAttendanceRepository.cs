using Dapper;
using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;
using StudentManagement.Infrastructure.Hybrid.Reliability;

namespace StudentManagement.Infrastructure.Hybrid.Repositories;

public class HybridAttendanceRepository : IAttendanceRepository
{
    private readonly HybridDbContext _context;
    private readonly ICurrentUserContext _currentUser;

    public HybridAttendanceRepository(
        HybridDbContext context,
        ICurrentUserContext currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    private int CurrentSchoolId =>
        _currentUser.SchoolId
        ?? throw new InvalidOperationException(
            "A school must be selected.");

    public Attendance? GetById(int id)
    {
        const string sql = """
            SELECT *
            FROM Attendances
            WHERE Id = @Id
            AND SchoolId = @SchoolId
            """;

        return _context.Connection.QuerySingleOrDefault<Attendance>(
            sql,
            new { Id = id, SchoolId = CurrentSchoolId });
    }

    public IEnumerable<Attendance> GetAll()
    {
        const string sql = """
            SELECT *
            FROM Attendances
            WHERE SchoolId = @SchoolId
            """;

        return _context.Connection.Query<Attendance>(
            sql,
            new { SchoolId = CurrentSchoolId });
    }

    public IEnumerable<Attendance> GetByStudentId(int studentId)
    {
        return DatabaseExecution.Execute(() =>
        {
            const string sql = """
                SELECT *
                FROM Attendances
                WHERE StudentId = @StudentId
                AND SchoolId = @SchoolId
                ORDER BY Date DESC
                """;

            return _context.Connection.Query<Attendance>(
                sql,
                new
                {
                    StudentId = studentId,
                    SchoolId = CurrentSchoolId
                });
        });
    }

    public IEnumerable<Attendance> GetByCourseAndDate(
        int courseId,
        DateTime date)
    {
        const string sql = """
            SELECT *
            FROM Attendances
            WHERE CourseId = @CourseId
            AND Date = @Date
            AND SchoolId = @SchoolId
            """;

        return _context.Connection.Query<Attendance>(
            sql,
            new
            {
                CourseId = courseId,
                Date = date.Date,
                SchoolId = CurrentSchoolId
            });
    }

    public Attendance? GetByStudentCourseAndDate(
        int studentId,
        int courseId,
        DateTime date)
    {
        const string sql = """
            SELECT *
            FROM Attendances
            WHERE StudentId = @StudentId
            AND CourseId = @CourseId
            AND Date = @Date
            AND SchoolId = @SchoolId
            """;

        return _context.Connection.QuerySingleOrDefault<Attendance>(
            sql,
            new
            {
                StudentId = studentId,
                CourseId = courseId,
                Date = date.Date,
                SchoolId = CurrentSchoolId
            });
    }

    public void Add(Attendance entity)
    {
        entity.AssignToSchool(CurrentSchoolId);
        _context.Attendances.Add(entity);
        _context.SaveChanges();
    }

    public void Update(Attendance entity)
    {
        if (entity.SchoolId != CurrentSchoolId)
            throw new UnauthorizedAccessException(
                "Attendance does not belong to the current school.");

        _context.Entry(entity).State = EntityState.Modified;
        _context.SaveChanges();
        _context.Entry(entity).State = EntityState.Detached;
    }

    public void Delete(int id)
    {
        var entity = _context.Attendances
            .SingleOrDefault(x =>
                x.Id == id &&
                x.SchoolId == CurrentSchoolId);

        if (entity is null)
            return;

        _context.Attendances.Remove(entity);
        _context.SaveChanges();
    }
}