namespace StudentManagement.Core.Models;

public class Course : BaseEntity
{
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; set; }
    public int DurationMonths { get; private set; }
    public decimal FeeAmount { get; private set; }

    public Course(string code, string name, int durationMonths, decimal feeAmount)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Course code cannot be blank.");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Course name is required.");
        if (durationMonths <= 0) throw new ArgumentOutOfRangeException(nameof(durationMonths), "Duration must be positive.");

        Code = code;
        Name = name;
        DurationMonths = durationMonths;
        ApplyPricingUpdate(feeAmount);
    }

    protected Course() { }

    public void ApplyPricingUpdate(decimal newAmount)
    {
        if (newAmount < 0) throw new ArgumentException("Pricing metrics cannot reflect negative balances.");
        FeeAmount = newAmount;
    }

    public void ModifyCourseDetails(string name, int durationMonths)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be wiped out.");
        if (durationMonths <= 0) throw new ArgumentException("Duration must match a logical window.");

        Name = name;
        DurationMonths = durationMonths;
    }

    public override string ToString() => $"({Code}) {Name} - {DurationMonths} Months | Base Rate: ${FeeAmount:F2}";
}