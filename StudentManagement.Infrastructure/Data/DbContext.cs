using Microsoft.Extensions.Configuration;
using System;

namespace StudentManagement.Infrastructure.Data
{
    public class DbContext
    {
        private readonly string _connectionString;

        // config loading for Console App
        public DbContext()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            var configuration = builder.Build();
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.json.");
        }

        // loading connection string dynamically MVC/WebAPI 
        public DbContext(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public string ConnectionString => _connectionString;
    }
}