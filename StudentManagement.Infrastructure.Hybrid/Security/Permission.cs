namespace StudentManagement.Infrastructure.Hybrid.Security;

public sealed class Permission
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}