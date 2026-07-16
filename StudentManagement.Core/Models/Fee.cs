using StudentManagement.Core.Enums;

namespace StudentManagement.Core.Models;

public class Fee : BaseEntity
{
    public int StudentId { get; init; }
    public Student? Student { get; set; }
    public int CourseId { get; init; }
    public Course? Course { get; set; }
    public decimal AmountDue { get; init; }
    public decimal AmountPaid { get; private set; }
    public DateTime? PaymentDate { get; private set; }
    public string? Remarks { get; private set; }

    // Dynamically computed Payment Status
    public PaymentStatus Status
    {
        get
        {
            if (AmountPaid <= 0) return PaymentStatus.Unpaid;
            if (AmountPaid >= AmountDue) return PaymentStatus.Paid;
            return PaymentStatus.Partial;
        }
    }

    public decimal RemainingBalance => AmountDue - AmountPaid;
    public bool IsFullySettled => RemainingBalance <= 0;

    public Fee(int studentId, int courseId, decimal amountDue)
    {
        if (amountDue <= 0) throw new ArgumentException("Initial base amount due must be positive.");

        StudentId = studentId;
        CourseId = courseId;
        AmountDue = amountDue;
        AmountPaid = 0.00m;
    }

    protected Fee() { }

    public void ProcessPayment(decimal amount, string? transactionRemarks)
    {
        if (amount <= 0) throw new ArgumentException("Payment amount must be greater than zero.");
        if (amount > RemainingBalance) throw new InvalidOperationException($"Overpayment error: The maximum remaining due is {RemainingBalance:C}.");

        AmountPaid += amount;
        PaymentDate = DateTime.UtcNow;
        Remarks = string.IsNullOrEmpty(transactionRemarks) ? Remarks : $"{Remarks} | Paid {amount:C} on {PaymentDate:d}: {transactionRemarks}";
    }

    public override string ToString() => $"Invoice #{Id} | Remaining Overdue balance: ${RemainingBalance:F2} (Paid: ${AmountPaid:F2}/${AmountDue:F2})";
}