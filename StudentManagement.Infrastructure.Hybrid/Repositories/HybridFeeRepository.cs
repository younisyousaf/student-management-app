using Dapper;
using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;

namespace StudentManagement.Infrastructure.Hybrid.Repositories;

public class HybridFeeRepository : IFeeRepository
{
    private readonly HybridDbContext _context;
    private readonly ICurrentUserContext _currentUser;

    public HybridFeeRepository(
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

    public Fee? GetById(int id)
    {
        const string sql = """
            SELECT *
            FROM Fees
            WHERE Id = @Id
            AND SchoolId = @SchoolId
            """;

        return _context.Connection.QuerySingleOrDefault<Fee>(
            sql,
            new { Id = id, SchoolId = CurrentSchoolId });
    }

    public IEnumerable<Fee> GetAll()
    {
        const string sql = """
            SELECT *
            FROM Fees
            WHERE SchoolId = @SchoolId
            """;

        return _context.Connection.Query<Fee>(
            sql,
            new { SchoolId = CurrentSchoolId });
    }

    public Fee? GetByStudentAndCourse(
        int studentId,
        int courseId)
    {
        const string sql = """
            SELECT *
            FROM Fees
            WHERE StudentId = @StudentId
            AND CourseId = @CourseId
            AND SchoolId = @SchoolId
            """;

        return _context.Connection.QuerySingleOrDefault<Fee>(
            sql,
            new
            {
                StudentId = studentId,
                CourseId = courseId,
                SchoolId = CurrentSchoolId
            });
    }

    public void Add(Fee entity)
    {
        entity.AssignToSchool(CurrentSchoolId);
        _context.Fees.Add(entity);
        _context.SaveChanges();
    }

    public void Update(Fee entity)
    {
        if (entity.SchoolId != CurrentSchoolId)
            throw new UnauthorizedAccessException(
                "Fee does not belong to the current school.");

        _context.Entry(entity).State = EntityState.Modified;
        _context.SaveChanges();
    }

    public void Delete(int id)
    {
        var entity = _context.Fees
            .SingleOrDefault(x =>
                x.Id == id &&
                x.SchoolId == CurrentSchoolId);

        if (entity is null)
            return;

        _context.Fees.Remove(entity);
        _context.SaveChanges();
    }
}