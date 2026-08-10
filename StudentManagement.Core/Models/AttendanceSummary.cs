namespace StudentManagement.Core.Models;

public record AttendanceSummary(
    int StudentId,
    int? CourseId,
    int TotalRecords,
    int PresentCount,
    int AbsentCount,
    int LateCount,
    int ExcusedCount,
    double AttendancePercentage
);