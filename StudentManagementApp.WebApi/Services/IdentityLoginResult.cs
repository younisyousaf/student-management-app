namespace StudentManagementApp.WebApi.Services;

public sealed record IdentityLoginResult(
    bool Succeeded,
    string? Token = null,
    string? Error = null);