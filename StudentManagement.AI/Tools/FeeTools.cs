using System.ComponentModel;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;

namespace StudentManagement.AI.Tools;

public record FeeSummary(int Id, int StudentId, int CourseId, decimal AmountDue, decimal AmountPaid, decimal RemainingBalance, string Status, DateTime? PaymentDate);

public record FeeLookupResult(bool Found, FeeSummary? Fee, string? Message);

public class FeeTools
{
    private readonly IFeeService _feeService;

    public FeeTools(IFeeService feeService)
    {
        _feeService = feeService;
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
        _feeService.ProcessStudentPayment(
            studentId,
            courseId,
            amount,
            remarks);

        return $"Payment of {amount:C} was successfully recorded " +
               $"for student ID {studentId} in course ID {courseId}.";
    }

    private static FeeSummary ToSummary(Fee fee) =>
        new(fee.Id, fee.StudentId, fee.CourseId, fee.AmountDue, fee.AmountPaid, fee.RemainingBalance, fee.Status.ToString(), fee.PaymentDate);
}