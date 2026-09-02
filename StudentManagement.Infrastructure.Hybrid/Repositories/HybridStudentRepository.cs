using Dapper;
using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;
using StudentManagement.Infrastructure.Hybrid.Reliability;

namespace StudentManagement.Infrastructure.Hybrid.Repositories;

public class HybridStudentRepository : IStudentRepository
{
    private readonly HybridDbContext _context;
    private readonly ICurrentUserContext _currentUser;

    public HybridStudentRepository(
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

    public Student? GetById(int id)
    {
        return DatabaseExecution.Execute(() =>
        {
            const string sql = """
                SELECT *
                FROM Students
                WHERE Id = @Id
                AND SchoolId = @SchoolId
                """;

            return _context.Connection
                .QuerySingleOrDefault<Student>(
                    sql,
                    new
                    {
                        Id = id,
                        SchoolId = CurrentSchoolId
                    });
        });
    }

    public IEnumerable<Student> GetAll()
    {
        const string sql = """
            SELECT *
            FROM Students
            WHERE SchoolId = @SchoolId
            """;

        return _context.Connection.Query<Student>(
            sql,
            new { SchoolId = CurrentSchoolId });
    }

    public Student? GetByRollNumber(string rollNumber)
    {
        const string sql = """
            SELECT *
            FROM Students
            WHERE RollNumber = @RollNumber
            AND SchoolId = @SchoolId
            """;

        return _context.Connection
            .QuerySingleOrDefault<Student>(
                sql,
                new
                {
                    RollNumber = rollNumber,
                    SchoolId = CurrentSchoolId
                });
    }

    public Student? GetByEmail(string email)
    {
        const string sql = """
            SELECT *
            FROM Students
            WHERE Email = @Email
            AND SchoolId = @SchoolId
            """;

        return _context.Connection
            .QuerySingleOrDefault<Student>(
                sql,
                new
                {
                    Email = email,
                    SchoolId = CurrentSchoolId
                });
    }

    public void Add(Student entity)
    {
        entity.AssignToSchool(CurrentSchoolId);

        _context.Students.Add(entity);
        _context.SaveChanges();
    }

    public void Update(Student entity)
    {
        if (entity.SchoolId != CurrentSchoolId)
            throw new UnauthorizedAccessException(
                "Student does not belong to the current school.");

        _context.Entry(entity).State = EntityState.Modified;
        _context.SaveChanges();
    }

    public void Delete(int id)
    {
        var entity = _context.Students
            .SingleOrDefault(x =>
                x.Id == id &&
                x.SchoolId == CurrentSchoolId);

        if (entity is null)
            return;

        _context.Students.Remove(entity);
        _context.SaveChanges();
    }
}