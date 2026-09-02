using Dapper;
using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;

namespace StudentManagement.Infrastructure.Hybrid.Repositories;

public class HybridEnrollmentRepository : IEnrollmentRepository
{
    private readonly HybridDbContext _context;
    private readonly ICurrentUserContext _currentUser;

    public HybridEnrollmentRepository(
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

    public Enrollment? GetById(int id)
    {
        const string sql = """
            SELECT *
            FROM Enrollments
            WHERE Id = @Id
            AND SchoolId = @SchoolId
            """;

        return _context.Connection
            .QuerySingleOrDefault<Enrollment>(
                sql,
                new { Id = id, SchoolId = CurrentSchoolId });
    }

    public IEnumerable<Enrollment> GetAll()
    {
        const string sql = """
            SELECT *
            FROM Enrollments
            WHERE SchoolId = @SchoolId
            """;

        return _context.Connection.Query<Enrollment>(
            sql,
            new { SchoolId = CurrentSchoolId });
    }

    public bool IsAlreadyEnrolled(
        int studentId,
        int courseId)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM Enrollments
            WHERE StudentId = @StudentId
            AND CourseId = @CourseId
            AND SchoolId = @SchoolId
            AND Status <> 'Dropped'
            """;

        return _context.Connection.ExecuteScalar<int>(
            sql,
            new
            {
                StudentId = studentId,
                CourseId = courseId,
                SchoolId = CurrentSchoolId
            }) > 0;
    }

    public IEnumerable<Enrollment> GetByStudentId(
        int studentId)
    {
        const string sql = """
            SELECT *
            FROM Enrollments
            WHERE StudentId = @StudentId
            AND SchoolId = @SchoolId
            """;

        return _context.Connection.Query<Enrollment>(
            sql,
            new
            {
                StudentId = studentId,
                SchoolId = CurrentSchoolId
            });
    }

    public void Add(Enrollment entity)
    {
        entity.AssignToSchool(CurrentSchoolId);
        _context.Enrollments.Add(entity);
        _context.SaveChanges();
    }

    public void Update(Enrollment entity)
    {
        if (entity.SchoolId != CurrentSchoolId)
            throw new UnauthorizedAccessException(
                "Enrollment does not belong to the current school.");

        _context.Entry(entity).State =
            EntityState.Modified;

        _context.SaveChanges();
    }

    public void Delete(int id)
    {
        var entity = _context.Enrollments
            .SingleOrDefault(x =>
                x.Id == id &&
                x.SchoolId == CurrentSchoolId);

        if (entity is null)
            return;

        _context.Enrollments.Remove(entity);
        _context.SaveChanges();
    }
}