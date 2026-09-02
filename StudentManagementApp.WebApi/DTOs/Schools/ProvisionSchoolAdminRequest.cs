namespace StudentManagementApp.WebApi.DTOs.Schools;

public sealed record ProvisionSchoolAdminRequest(
    string Username,
    string Email,
    string Password);
