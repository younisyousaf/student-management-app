using StudentManagement.Core.Models;

namespace StudentManagement.Core.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        User? GetByUsername(string username);
        User? GetByEmail(string email);
    }
}