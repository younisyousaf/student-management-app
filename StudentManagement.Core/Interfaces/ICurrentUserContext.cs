namespace StudentManagement.Core.Interfaces;

public interface ICurrentUserContext
{
    int? UserId { get; }
    string? Username { get; }
    string? Email { get; }
    IReadOnlyCollection<string> Roles { get; }
    int? SchoolId { get; }
    bool IsAuthenticated { get; }
}