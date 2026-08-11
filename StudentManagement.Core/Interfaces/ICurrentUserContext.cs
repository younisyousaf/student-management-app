namespace StudentManagement.Core.Interfaces;

public interface ICurrentUserContext
{
    int? UserId { get; }

    string? Username { get; }

    string? Email { get; }

    string? Role { get; }

    bool IsAuthenticated { get; }
}