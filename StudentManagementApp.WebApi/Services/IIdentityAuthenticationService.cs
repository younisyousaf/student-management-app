namespace StudentManagementApp.WebApi.Services;

public interface IIdentityAuthenticationService
{
    Task<IdentityLoginResult> LoginAsync(string username, string password);
}