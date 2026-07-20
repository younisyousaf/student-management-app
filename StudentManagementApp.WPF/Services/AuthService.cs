namespace StudentManagementApp.WPF.Services
{
    public class AuthService
    {
        // Singleton instance so all Windows can check if the user is authenticated
        public static AuthService Instance { get; } = new AuthService();

        public string? JwtToken { get; set; }
        public bool IsAuthenticated => !string.IsNullOrEmpty(JwtToken);

        private AuthService() { }

        public void Logout() => JwtToken = null;
    }
}