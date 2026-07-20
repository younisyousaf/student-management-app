using StudentManagement.Core.Models;

namespace StudentManagement.Core.Interfaces
{
    public interface IUserService
    {
        void RegisterUser(User user, string plainTextPassword);
        string? Login(string username, string password); // Returns JWT
        User? GetUserById(int id);
    }
}