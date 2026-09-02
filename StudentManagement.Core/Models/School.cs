namespace StudentManagement.Core.Models;

public class School : BaseEntity
{
    public string Name { get; private set; }
    public string Code { get; private set; }
    public string TimeZoneId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public School(string name, string code, string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("School name is required.");
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("School code is required.");
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new ArgumentException("Timezone is required.");

        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        TimeZoneId = timeZoneId.Trim();
    }

    protected School() { }

    public void Update(string name, string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("School name is required.");
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new ArgumentException("Timezone is required.");

        Name = name.Trim();
        TimeZoneId = timeZoneId.Trim();
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}