using System.ComponentModel;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;

namespace StudentManagement.AI.Tools;

public record FeeSummary(
    int Id,
    int StudentId,
    string StudentName,
    string RollNumber,
    int CourseId,
    string CourseName,
    string CourseCode,
    decimal AmountDue,
    decimal AmountPaid,
    decimal RemainingBalance,
    string Status,
    DateTime? PaymentDate);

public record FeeLookupResult(bool Found, FeeSummary? Fee, string? Message);

public class FeeTools
{
    private readonly IFeeService _feeService;
    private readonly IStudentService _studentService;
    private readonly ICourseService _courseService;

    public FeeTools(
        IFeeService feeService,
        IStudentService studentService,
        ICourseService courseService)
    {
        _feeService =
            feeService;

        _studentService =
            studentService;

        _courseService =
            courseService;
    }

    [Description("Get a fee record by its ID. Always check the Found field first — if Found is false, no fee record with that ID exists.")]
    public FeeLookupResult GetFeeById(
        [Description("The fee record's ID")] int feeId)
    {
        var fee = _feeService.GetFeeById(feeId);
        return fee == null
            ? new FeeLookupResult(false, null, $"No fee record exists with ID {feeId}.")
            : new FeeLookupResult(true, ToSummary(fee), null);
    }

    [Description("Get a student's fee statement for a specific course — shows amount due, amount paid, remaining balance, and payment status. " +
        "Always check the Found field first — if Found is false, no fee statement exists for that student/course combination, " +
        "which usually means the student isn't enrolled in that course or no fee has been generated yet.")]
    public FeeLookupResult GetFeeStatement(
        [Description("The student's internal numeric ID")] int studentId,
        [Description("The course's internal numeric ID")] int courseId)
    {
        var fee = _feeService.GetFeeStatement(studentId, courseId);
        return fee == null
            ? new FeeLookupResult(false, null, $"No fee statement exists for student {studentId} in course {courseId}.")
            : new FeeLookupResult(true, ToSummary(fee), null);
    }

    [Description(
    "Get all fee records for a specific student across their courses. " +
    "Use this when the user asks about the student's overall fee " +
    "obligations, paid fees, unpaid fees, or outstanding balances.")]
    public IEnumerable<FeeSummary>
    GetFeesForStudent(
        [Description(
            "The exact internal student ID.")]
        int studentId)
    {
        if (studentId <= 0)
        {
            return [];
        }

        return _feeService
            .GetAllFeeLedgers()
            .Where(
                fee =>
                    fee.StudentId ==
                    studentId)
            .Take(50)
            .Select(ToSummary);
    }

    [Description(
    "Process a payment for a student's course fee. " +
    "This modifies financial data and must only execute after human approval. " +
    "Before using this tool, verify the exact student and course, and retrieve the fee statement.")]
    public string ProcessStudentPayment(
    [Description("The exact internal student ID.")]
    int studentId,

    [Description("The exact internal course ID.")]
    int courseId,

    [Description("The payment amount.")]
    decimal amount,

    [Description("Optional payment remarks.")]
    string? remarks = null)
    {
        var student =
         _studentService.GetStudentById(
             studentId);

        var course =
            _courseService.GetCourseById(
                courseId);

        _feeService.ProcessStudentPayment(
            studentId,
            courseId,
            amount,
            remarks);

        return
            $"A payment of {amount:N2} was successfully recorded " +
            $"for {student?.FullName ?? "the student"} " +
            $"for {course?.Name ?? "the course"}.";
    }

    private FeeSummary ToSummary(Fee fee)
    {
        var student =
            _studentService.GetStudentById(fee.StudentId);

        var course =
            _courseService.GetCourseById(fee.CourseId);

        return new FeeSummary(
            Id:
                fee.Id,

            StudentId:
                fee.StudentId,

            StudentName:
                student?.FullName ??
                "Unknown student",

            RollNumber:
                student?.RollNumber ??
                string.Empty,

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
                fee.Status.ToString(),

            PaymentDate:
                fee.PaymentDate);
    }

}