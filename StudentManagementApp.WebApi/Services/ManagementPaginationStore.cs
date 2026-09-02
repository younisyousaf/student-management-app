using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;
using StudentManagement.Infrastructure.Hybrid;

namespace StudentManagementApp.WebApi.Services;

public sealed class ManagementPaginationStore
{
    private readonly HybridDbContext _dbContext;
    private readonly ICurrentUserContext _currentUser;

    public ManagementPaginationStore(
        HybridDbContext dbContext,
        ICurrentUserContext currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public Task<PaginatedResult<Student>> GetStudentsAsync(
    int pageNumber,
    int pageSize,
    CancellationToken cancellationToken = default)
    {
        var schoolId = _currentUser.SchoolId
            ?? throw new InvalidOperationException(
                "A school must be selected.");

        return GetPageAsync(
            _dbContext
                .Students
                .AsNoTracking()
                .Where(student =>
                    student.SchoolId == schoolId)
                .OrderBy(student =>
                    student.Id),
            pageNumber,
            pageSize,
            cancellationToken);
    }

    public Task<PaginatedResult<Course>> GetCoursesAsync(
    int pageNumber,
    int pageSize,
    CancellationToken cancellationToken = default)
    {
        var schoolId = _currentUser.SchoolId
            ?? throw new InvalidOperationException(
                "A school must be selected.");

        return GetPageAsync(
            _dbContext
                .Courses
                .AsNoTracking()
                .Where(course =>
                    course.SchoolId == schoolId)
                .OrderBy(course =>
                    course.Id),
            pageNumber,
            pageSize,
            cancellationToken);
    }

    public Task<PaginatedResult<Enrollment>> GetEnrollmentsAsync(
    int pageNumber,
    int pageSize,
    CancellationToken cancellationToken = default)
    {
        var schoolId = _currentUser.SchoolId
            ?? throw new InvalidOperationException(
                "A school must be selected.");

        return GetPageAsync(
            _dbContext.Enrollments
                .AsNoTracking()
                .Where(x => x.SchoolId == schoolId)
                .OrderBy(x => x.Id),
            pageNumber,
            pageSize,
            cancellationToken);
    }

    public Task<PaginatedResult<Attendance>> GetAttendanceAsync(
    int pageNumber,
    int pageSize,
    CancellationToken cancellationToken = default)
    {
        var schoolId = _currentUser.SchoolId
            ?? throw new InvalidOperationException(
                "A school must be selected.");

        return GetPageAsync(
            _dbContext.Attendances
                .AsNoTracking()
                .Where(a => a.SchoolId == schoolId)
                .OrderByDescending(a => a.Date)
                .ThenByDescending(a => a.Id),
            pageNumber,
            pageSize,
            cancellationToken);
    }

    public Task<PaginatedResult<Fee>> GetFeesAsync(
    int pageNumber,
    int pageSize,
    CancellationToken cancellationToken = default)
    {
        var schoolId = _currentUser.SchoolId
            ?? throw new InvalidOperationException(
                "A school must be selected.");

        return GetPageAsync(
            _dbContext.Fees
                .AsNoTracking()
                .Where(fee => fee.SchoolId == schoolId)
                .OrderBy(fee => fee.Id),
            pageNumber,
            pageSize,
            cancellationToken);
    }

    private static async Task<
        PaginatedResult<T>>
        GetPageAsync<T>(
            IQueryable<T> query,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
    {
        int totalCount =
            await query.CountAsync(
                cancellationToken);

        int skip =
            (pageNumber - 1) *
            pageSize;

        var items =
            await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(
                    cancellationToken);

        return new PaginatedResult<T>(
            items,
            pageNumber,
            pageSize,
            totalCount);
    }
}
