using System.ComponentModel;

namespace StudentManagement.AI.Tools.Hosted;

public sealed class HostedFeeTools
{
    private readonly ScopedToolExecutor _executor;

    public HostedFeeTools(
        ScopedToolExecutor executor)
    {
        _executor = executor;
    }

    [Description(
        "Get a fee record by its exact ID. " +
        "Always check Found first.")]
    public FeeLookupResult GetFeeById(
        [Description("The fee record ID.")]
        int feeId)
    {
        return _executor.Execute<
            FeeTools,
            FeeLookupResult>(
                tools =>
                    tools.GetFeeById(feeId));
    }

    [Description(
        "Get a student's fee statement for a specific course.")]
    public FeeLookupResult GetFeeStatement(
        [Description("The student's internal numeric ID.")]
        int studentId,

        [Description("The course's internal numeric ID.")]
        int courseId)
    {
        return _executor.Execute<
            FeeTools,
            FeeLookupResult>(
                tools =>
                    tools.GetFeeStatement(
                        studentId,
                        courseId));
    }

    [Description(
        "Process a payment against a student's course fee. " +
        "This modifies financial data and requires human approval.")]
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
        return _executor.Execute<
            FeeTools,
            string>(
                tools =>
                    tools.ProcessStudentPayment(
                        studentId,
                        courseId,
                        amount,
                        remarks));
    }
}
