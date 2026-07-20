namespace StudentManagement.Core.Models
{
    public class User : BaseEntity
    {
        public string Username { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; set; }
        public string Role { get; private set; }

        public bool IsActive { get; private set; } = true;

        // Protected parameterless constructor for EF Core tracking
        protected User() { }

        // Public constructor for secure domain instantiations
        public User(string username, string email, string role)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be empty.");
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty.");

            Username = username;
            Email = email;
            Role = string.IsNullOrWhiteSpace(role) ? "Admin" : role;
            IsActive = true;
        }
    }
}