using Dapper;
using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;

namespace StudentManagement.Infrastructure.Hybrid.Repositories
{
    public class HybridUserRepository : IUserRepository
    {
        private readonly HybridDbContext _context;

        public HybridUserRepository(HybridDbContext context)
        {
            _context = context;
        }

        public User? GetById(int id)
        {
            string sql = "SELECT * FROM Users WHERE Id = @Id";
            return _context.Connection.QuerySingleOrDefault<User>(sql, new { Id = id });
        }

        public IEnumerable<User> GetAll()
        {
            string sql = "SELECT * FROM Users";
            return _context.Connection.Query<User>(sql);
        }

        public User? GetByUsername(string username)
        {
            string sql = "SELECT * FROM Users WHERE Username = @Username";
            return _context.Connection.QuerySingleOrDefault<User>(sql, new { Username = username });
        }

        public User? GetByEmail(string email)
        {
            string sql = "SELECT * FROM Users WHERE Email = @Email";
            return _context.Connection.QuerySingleOrDefault<User>(sql, new { Email = email });
        }

        public void Add(User entity)
        {
            _context.Users.Add(entity);
            _context.SaveChanges();
        }

        public void Update(User entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var entity = _context.Users.Find(id);
            if (entity != null)
            {
                _context.Users.Remove(entity);
                _context.SaveChanges();
            }
        }
    }
}