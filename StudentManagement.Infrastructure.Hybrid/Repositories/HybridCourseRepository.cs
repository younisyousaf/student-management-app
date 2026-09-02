using Dapper;
using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;

namespace StudentManagement.Infrastructure.Hybrid.Repositories;

public class HybridCourseRepository : ICourseRepository
{
    private readonly HybridDbContext _context;
    private readonly ICurrentUserContext _currentUser;

    public HybridCourseRepository(
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

    public Course? GetById(int id)
    {
        const string sql = """
            SELECT *
            FROM Courses
            WHERE Id = @Id
            AND SchoolId = @SchoolId
            """;

        return _context.Connection
            .QuerySingleOrDefault<Course>(
                sql,
                new
                {
                    Id = id,
                    SchoolId = CurrentSchoolId
                });
    }

    public IEnumerable<Course> GetAll()
    {
        const string sql = """
            SELECT *
            FROM Courses
            WHERE SchoolId = @SchoolId
            """;

        return _context.Connection.Query<Course>(
            sql,
            new { SchoolId = CurrentSchoolId });
    }

    public Course? GetByCode(string code)
    {
        const string sql = """
            SELECT *
            FROM Courses
            WHERE Code = @Code
            AND SchoolId = @SchoolId
            """;

        return _context.Connection
            .QuerySingleOrDefault<Course>(
                sql,
                new
                {
                    Code = code,
                    SchoolId = CurrentSchoolId
                });
    }

    public void Add(Course entity)
    {
        entity.AssignToSchool(CurrentSchoolId);

        _context.Courses.Add(entity);
        _context.SaveChanges();
    }

    public void Update(Course entity)
    {
        if (entity.SchoolId != CurrentSchoolId)
            throw new UnauthorizedAccessException(
                "Course does not belong to the current school.");

        _context.Entry(entity).State = EntityState.Modified;
        _context.SaveChanges();
        _context.Entry(entity).State = EntityState.Detached;
    }

    public void Delete(int id)
    {
        var entity = _context.Courses
            .SingleOrDefault(x =>
                x.Id == id &&
                x.SchoolId == CurrentSchoolId);

        if (entity is null)
            return;

        _context.Courses.Remove(entity);
        _context.SaveChanges();
    }
}