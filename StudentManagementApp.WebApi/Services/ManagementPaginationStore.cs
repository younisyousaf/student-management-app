using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Models;
using StudentManagement.Infrastructure.Hybrid;

namespace StudentManagementApp.WebApi.Services;

public sealed class ManagementPaginationStore
{
    private readonly HybridDbContext _dbContext;

    public ManagementPaginationStore(
        HybridDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PaginatedResult<Student>>
        GetStudentsAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        return GetPageAsync(
            _dbContext
                .Students
                .AsNoTracking()
                .OrderBy(student => student.Id),
            pageNumber,
            pageSize,
            cancellationToken);
    }

    public Task<PaginatedResult<Course>>
        GetCoursesAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        return GetPageAsync(
            _dbContext
                .Courses
                .AsNoTracking()
                .OrderBy(course => course.Id),
            pageNumber,
            pageSize,
            cancellationToken);
    }

    public Task<PaginatedResult<Enrollment>>
        GetEnrollmentsAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        return GetPageAsync(
            _dbContext
                .Enrollments
                .AsNoTracking()
                .OrderBy(enrollment => enrollment.Id),
            pageNumber,
            pageSize,
            cancellationToken);
    }

    public Task<PaginatedResult<Attendance>>
        GetAttendanceAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        return GetPageAsync(
            _dbContext
                .Attendances
                .AsNoTracking()
                .OrderByDescending(
                    attendance => attendance.Date)
                .ThenByDescending(
                    attendance => attendance.Id),
            pageNumber,
            pageSize,
            cancellationToken);
    }

    public Task<PaginatedResult<Fee>>
        GetFeesAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        return GetPageAsync(
            _dbContext
                .Fees
                .AsNoTracking()
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
