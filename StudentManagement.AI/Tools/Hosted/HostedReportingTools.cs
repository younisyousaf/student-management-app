using System.ComponentModel;

namespace StudentManagement.AI.Tools.Hosted;

public sealed class HostedReportingTools
{
    private readonly ScopedToolExecutor _executor;

    public HostedReportingTools(
        ScopedToolExecutor executor)
    {
        _executor = executor;
    }

    [Description(
        "Get students whose attendance percentage is below a supplied threshold. " +
        "Students without attendance records are excluded. " +
        "Optionally scope the report to one course.")]
    public IReadOnlyList<LowAttendanceStudentReport>
        GetStudentsBelowAttendanceThreshold(
            [Description(
                "Percentage threshold, for example 75.")]
            double thresholdPercentage,

            [Description(
                "Optional exact internal course ID.")]
            int? courseId = null)
    {
        return _executor.Execute<
            ReportingTools,
            IReadOnlyList<LowAttendanceStudentReport>>(
                tools =>
                    tools
                        .GetStudentsBelowAttendanceThreshold(
                            thresholdPercentage,
                            courseId));
    }

    [Description(
        "Get students who currently have outstanding fee balances, " +
        "including their affected courses and aggregate outstanding totals.")]
    public IReadOnlyList<OutstandingFeeStudentReport>
        GetStudentsWithOutstandingFees()
    {
        return _executor.Execute<
            ReportingTools,
            IReadOnlyList<OutstandingFeeStudentReport>>(
                tools =>
                    tools
                        .GetStudentsWithOutstandingFees());
    }

    [Description(
        "Get aggregate attendance statistics for one specific course.")]
    public CourseAttendanceReport?
        GetCourseAttendanceSummary(
            [Description(
                "The exact internal course ID.")]
            int courseId)
    {
        return _executor.Execute<
            ReportingTools,
            CourseAttendanceReport?>(
                tools =>
                    tools
                        .GetCourseAttendanceSummary(
                            courseId));
    }

    [Description(
    "Get students who have no recorded attendance. " +
    "Optionally scope the report to active students in one course.")]
    public IReadOnlyList<StudentWithoutAttendanceReport>
    GetStudentsWithNoAttendanceRecords(
        [Description(
            "Optional exact internal course ID.")]
        int? courseId = null)
    {
        return _executor.Execute<
            ReportingTools,
            IReadOnlyList<StudentWithoutAttendanceReport>>(
                tools =>
                    tools.GetStudentsWithNoAttendanceRecords(
                        courseId));
    }

    [Description(
        "Get students who currently have no active course enrollment.")]
    public IReadOnlyList<StudentWithoutActiveEnrollmentReport>
        GetStudentsWithNoActiveEnrollment()
    {
        return _executor.Execute<
            ReportingTools,
            IReadOnlyList<StudentWithoutActiveEnrollmentReport>>(
                tools =>
                    tools.GetStudentsWithNoActiveEnrollment());
    }

    [Description(
        "Get institution-wide fee collection and outstanding balance statistics.")]
    public InstitutionFeeSummaryReport
        GetInstitutionFeeSummary()
    {
        return _executor.Execute<
            ReportingTools,
            InstitutionFeeSummaryReport>(
                tools =>
                    tools.GetInstitutionFeeSummary());
    }
}