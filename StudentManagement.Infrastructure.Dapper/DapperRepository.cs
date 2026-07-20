using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace StudentManagement.Infrastructure.Dapper
{
    public abstract class DapperRepository
    {
        private readonly string _connectionString;

        protected DapperRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        // Helper method to generate and open a database connection
        protected IDbConnection CreateConnection()
        {
            var connection = new SqlConnection(_connectionString);
            return connection;
        }
    }
}