using System.ComponentModel;
using StudentManagement.Core.Interfaces;

namespace StudentManagement.AI.Tools;

public record LowAttendanceStudentReport(
    int StudentId,
    string StudentName,
    string RollNumber,
    int? CourseId,
    string? CourseName,
    string? CourseCode,
    int TotalRecords,
    int PresentCount,
    int AbsentCount,
    int LateCount,
    int ExcusedCount,
    double AttendancePercentage);

public record OutstandingFeeCourseReport(
    int CourseId,
    string CourseName,
    string CourseCode,
    decimal AmountDue,
    decimal AmountPaid,
    decimal RemainingBalance,
    string Status);

public record OutstandingFeeStudentReport(
    int StudentId,
    string StudentName,
    string RollNumber,
    decimal TotalAmountDue,
    decimal TotalAmountPaid,
    decimal TotalOutstanding,
    IReadOnlyList<OutstandingFeeCourseReport> Courses);

public record CourseAttendanceReport(
    int CourseId,
    string CourseName,
    string CourseCode,
    int StudentsWithAttendanceRecords,
    int TotalRecords,
    int PresentCount,
    int AbsentCount,
    int LateCount,
    int ExcusedCount,
    double AttendancePercentage);

public record StudentWithoutAttendanceReport(
    int StudentId,
    string StudentName,
    string RollNumber,
    int? CourseId,
    string? CourseName,
    string? CourseCode);

public record StudentWithoutActiveEnrollmentReport(
    int StudentId,
    string StudentName,
    string RollNumber);

public record InstitutionFeeSummaryReport(
    int TotalFeeRecords,
    int StudentsWithFeeRecords,
    int StudentsWithOutstandingBalance,
    decimal TotalAmountDue,
    decimal TotalAmountPaid,
    decimal TotalOutstanding,
    double CollectionPercentage,
    int PaidFeeRecords,
    int PartialFeeRecords,
    int UnpaidFeeRecords);

public sealed class ReportingTools
{
    private readonly IStudentService _studentService;
    private readonly ICourseService _courseService;
    private readonly IEnrollmentService _enrollmentService;
    private readonly IAttendanceService _attendanceService;
    private readonly IFeeService _feeService;

    public ReportingTools(
        IStudentService studentService,
        ICourseService courseService,
        IEnrollmentService enrollmentService,
        IAttendanceService attendanceService,
        IFeeService feeService)
    {
        _studentService = studentService;
        _courseService = courseService;
        _enrollmentService = enrollmentService;
        _attendanceService = attendanceService;
        _feeService = feeService;
    }

    [Description(
        "Get students whose calculated attendance percentage is below a supplied threshold. " +
        "Use this for queries such as 'list students below 75 percent attendance'. " +
        "Students with no attendance records are excluded because no attendance percentage " +
        "has actually been established for them. " +
        "Optionally limit the calculation to one course.")]
    public IReadOnlyList<LowAttendanceStudentReport>
        GetStudentsBelowAttendanceThreshold(
            [Description(
                "Attendance percentage threshold. " +
                "For example, supply 75 for students below 75 percent.")]
            double thresholdPercentage,

            [Description(
                "Optional exact internal course ID. " +
                "Omit to calculate attendance across all courses.")]
            int? courseId = null)
    {
        if (
            thresholdPercentage < 0 ||
            thresholdPercentage > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(thresholdPercentage),
                "Attendance threshold must be between 0 and 100.");
        }

        string? courseName = null;
        string? courseCode = null;

        if (courseId.HasValue)
        {
            var course =
                _courseService.GetCourseById(
                    courseId.Value);

            if (course is null)
            {
                return [];
            }

            courseName =
                course.Name;

            courseCode =
                course.Code;
        }

        var results =
            new List<LowAttendanceStudentReport>();

        foreach (
            var student
            in _studentService.GetAllStudents())
        {
            var summary =
                _attendanceService
                    .GetAttendanceSummary(
                        student.Id,
                        courseId);

            /*
             * No records does NOT mean 0% attendance.
             * It means there is not enough attendance data
             * to calculate a meaningful percentage.
             */
            if (summary.TotalRecords == 0)
            {
                continue;
            }

            if (
                summary.AttendancePercentage >=
                thresholdPercentage)
            {
                continue;
            }

            results.Add(
                new LowAttendanceStudentReport(
                    StudentId:
                        student.Id,

                    StudentName:
                        student.FullName,

                    RollNumber:
                        student.RollNumber,

                    CourseId:
                        courseId,

                    CourseName:
                        courseName,

                    CourseCode:
                        courseCode,

                    TotalRecords:
                        summary.TotalRecords,

                    PresentCount:
                        summary.PresentCount,

                    AbsentCount:
                        summary.AbsentCount,

                    LateCount:
                        summary.LateCount,

                    ExcusedCount:
                        summary.ExcusedCount,

                    AttendancePercentage:
                        summary.AttendancePercentage));
        }

        return results
            .OrderBy(
                result =>
                    result.AttendancePercentage)
            .ThenBy(
                result =>
                    result.StudentName)
            .Take(100)
            .ToList();
    }

    [Description(
        "Get students who currently have an outstanding fee balance. " +
        "Returns one result per student with their outstanding courses and totals. " +
        "Use this for queries such as 'which students still owe fees?' or " +
        "'list students with unpaid balances'.")]
    public IReadOnlyList<OutstandingFeeStudentReport>
        GetStudentsWithOutstandingFees()
    {
        var outstandingFees =
            _feeService
                .GetAllFeeLedgers()
                .Where(
                    fee =>
                        fee.RemainingBalance > 0)
                .ToList();

        var results =
            new List<OutstandingFeeStudentReport>();

        foreach (
            var studentGroup
            in outstandingFees
                .GroupBy(
                    fee =>
                        fee.StudentId))
        {
            var student =
                _studentService.GetStudentById(
                    studentGroup.Key);

            if (student is null)
            {
                continue;
            }

            var courseReports =
                studentGroup
                    .Select(
                        fee =>
                        {
                            var course =
                                _courseService
                                    .GetCourseById(
                                        fee.CourseId);

                            return
                                new OutstandingFeeCourseReport(
                                    CourseId:
                                        fee.CourseId,

                                    CourseName:
                                        course?.Name ??
                                        "Unknown course",

                                    CourseCode:
                                        course?.Code ??
                                        string.Empty,

                                    AmountDue:
                                        fee.AmountDue,

                                    AmountPaid:
                                        fee.AmountPaid,

                                    RemainingBalance:
                                        fee.RemainingBalance,

                                    Status:
                                        fee.Status.ToString());
                        })
                    .OrderByDescending(
                        fee =>
                            fee.RemainingBalance)
                    .ToList();

            results.Add(
                new OutstandingFeeStudentReport(
                    StudentId:
                        student.Id,

                    StudentName:
                        student.FullName,

                    RollNumber:
                        student.RollNumber,

                    TotalAmountDue:
                        studentGroup.Sum(
                            fee =>
                                fee.AmountDue),

                    TotalAmountPaid:
                        studentGroup.Sum(
                            fee =>
                                fee.AmountPaid),

                    TotalOutstanding:
                        studentGroup.Sum(
                            fee =>
                                fee.RemainingBalance),

                    Courses:
                        courseReports));
        }

        return results
            .OrderByDescending(
                result =>
                    result.TotalOutstanding)
            .ThenBy(
                result =>
                    result.StudentName)
            .Take(100)
            .ToList();
    }

    [Description(
        "Get an aggregate attendance summary for a specific course across all recorded attendance. " +
        "Use this for questions about the overall attendance situation of one course. " +
        "The percentage uses the same application rule as student attendance: " +
        "Present and Late count as attended, while Absent and Excused do not.")]
    public CourseAttendanceReport?
        GetCourseAttendanceSummary(
            [Description(
                "The exact internal course ID.")]
            int courseId)
    {
        if (courseId <= 0)
        {
            return null;
        }

        var course =
            _courseService.GetCourseById(
                courseId);

        if (course is null)
        {
            return null;
        }

        var records =
            _attendanceService
                .GetAllAttendance()
                .Where(
                    attendance =>
                        attendance.CourseId ==
                        courseId)
                .ToList();

        int total =
            records.Count;

        int present =
            records.Count(
                record =>
                    record.Status.ToString() ==
                    "Present");

        int absent =
            records.Count(
                record =>
                    record.Status.ToString() ==
                    "Absent");

        int late =
            records.Count(
                record =>
                    record.Status.ToString() ==
                    "Late");

        int excused =
            records.Count(
                record =>
                    record.Status.ToString() ==
                    "Excused");

        int countedPresent =
            present + late;

        double percentage =
            total == 0
                ? 0
                : Math.Round(
                    (double)countedPresent /
                    total *
                    100,
                    2);

        int studentsWithRecords =
            records
                .Select(
                    record =>
                        record.StudentId)
                .Distinct()
                .Count();

        return new CourseAttendanceReport(
            CourseId:
                course.Id,

            CourseName:
                course.Name,

            CourseCode:
                course.Code,

            StudentsWithAttendanceRecords:
                studentsWithRecords,

            TotalRecords:
                total,

            PresentCount:
                present,

            AbsentCount:
                absent,

            LateCount:
                late,

            ExcusedCount:
                excused,

            AttendancePercentage:
                percentage);
    }

    [Description(
    "Get students who have no attendance records. " +
    "When courseId is omitted, returns students with no attendance records anywhere. " +
    "When courseId is supplied, returns active students in that course who have no attendance records for that course.")]
    public IReadOnlyList<StudentWithoutAttendanceReport>
    GetStudentsWithNoAttendanceRecords(
        [Description(
            "Optional exact internal course ID. " +
            "Omit to check attendance across all courses.")]
        int? courseId = null)
    {
        var students =
            _studentService
                .GetAllStudents()
                .ToList();

        var attendanceRecords =
            _attendanceService
                .GetAllAttendance()
                .ToList();

        if (!courseId.HasValue)
        {
            var studentsWithAttendance =
                attendanceRecords
                    .Select(record => record.StudentId)
                    .ToHashSet();

            return students
                .Where(student =>
                    !studentsWithAttendance.Contains(
                        student.Id))
                .Select(student =>
                    new StudentWithoutAttendanceReport(
                        StudentId: student.Id,
                        StudentName: student.FullName,
                        RollNumber: student.RollNumber,
                        CourseId: null,
                        CourseName: null,
                        CourseCode: null))
                .OrderBy(result => result.StudentName)
                .Take(100)
                .ToList();
        }

        var course =
            _courseService.GetCourseById(
                courseId.Value);

        if (course is null)
        {
            return [];
        }

        var activelyEnrolledStudentIds =
            _enrollmentService
                .GetAllEnrollments()
                .Where(enrollment =>
                    enrollment.CourseId == courseId.Value &&
                    string.Equals(
                        enrollment.Status,
                        "Active",
                        StringComparison.OrdinalIgnoreCase))
                .Select(enrollment => enrollment.StudentId)
                .ToHashSet();

        var studentsWithCourseAttendance =
            attendanceRecords
                .Where(record =>
                    record.CourseId == courseId.Value)
                .Select(record => record.StudentId)
                .ToHashSet();

        return students
            .Where(student =>
                activelyEnrolledStudentIds.Contains(student.Id) &&
                !studentsWithCourseAttendance.Contains(student.Id))
            .Select(student =>
                new StudentWithoutAttendanceReport(
                    StudentId: student.Id,
                    StudentName: student.FullName,
                    RollNumber: student.RollNumber,
                    CourseId: course.Id,
                    CourseName: course.Name,
                    CourseCode: course.Code))
            .OrderBy(result => result.StudentName)
            .Take(100)
            .ToList();
    }

    [Description(
    "Get students who currently have no active course enrollment. " +
    "Dropped and completed enrollments do not count as active.")]
    public IReadOnlyList<StudentWithoutActiveEnrollmentReport>
    GetStudentsWithNoActiveEnrollment()
    {
        var activeStudentIds =
            _enrollmentService
                .GetAllEnrollments()
                .Where(enrollment =>
                    string.Equals(
                        enrollment.Status,
                        "Active",
                        StringComparison.OrdinalIgnoreCase))
                .Select(enrollment =>
                    enrollment.StudentId)
                .ToHashSet();

        return _studentService
            .GetAllStudents()
            .Where(student =>
                !activeStudentIds.Contains(
                    student.Id))
            .Select(student =>
                new StudentWithoutActiveEnrollmentReport(
                    StudentId: student.Id,
                    StudentName: student.FullName,
                    RollNumber: student.RollNumber))
            .OrderBy(result =>
                result.StudentName)
            .Take(100)
            .ToList();
    }

    [Description(
    "Get institution-wide fee collection statistics including total due, " +
    "total paid, total outstanding, collection percentage, and counts of " +
    "paid, partially paid, and unpaid fee records.")]
    public InstitutionFeeSummaryReport
    GetInstitutionFeeSummary()
    {
        var fees =
            _feeService
                .GetAllFeeLedgers()
                .ToList();

        decimal totalDue =
            fees.Sum(fee =>
                fee.AmountDue);

        decimal totalPaid =
            fees.Sum(fee =>
                fee.AmountPaid);

        decimal totalOutstanding =
            fees.Sum(fee =>
                fee.RemainingBalance);

        double collectionPercentage =
            totalDue <= 0
                ? 0
                : Math.Round(
                    (double)(
                        totalPaid /
                        totalDue *
                        100),
                    2);

        int paidFeeRecords =
            fees.Count(fee =>
                fee.RemainingBalance <= 0);

        int partialFeeRecords =
            fees.Count(fee =>
                fee.AmountPaid > 0 &&
                fee.RemainingBalance > 0);

        int unpaidFeeRecords =
            fees.Count(fee =>
                fee.AmountPaid <= 0 &&
                fee.RemainingBalance > 0);

        int studentsWithFeeRecords =
            fees
                .Select(fee =>
                    fee.StudentId)
                .Distinct()
                .Count();

        int studentsWithOutstandingBalance =
            fees
                .Where(fee =>
                    fee.RemainingBalance > 0)
                .Select(fee =>
                    fee.StudentId)
                .Distinct()
                .Count();

        return new InstitutionFeeSummaryReport(
            TotalFeeRecords:
                fees.Count,

            StudentsWithFeeRecords:
                studentsWithFeeRecords,

            StudentsWithOutstandingBalance:
                studentsWithOutstandingBalance,

            TotalAmountDue:
                totalDue,

            TotalAmountPaid:
                totalPaid,

            TotalOutstanding:
                totalOutstanding,

            CollectionPercentage:
                collectionPercentage,

            PaidFeeRecords:
                paidFeeRecords,

            PartialFeeRecords:
                partialFeeRecords,

            UnpaidFeeRecords:
                unpaidFeeRecords);
    }
}