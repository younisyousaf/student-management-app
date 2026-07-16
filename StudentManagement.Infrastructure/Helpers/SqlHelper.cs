using Microsoft.Data.SqlClient;
using StudentManagement.Infrastructure.Data;
using System.Data;

namespace StudentManagement.Infrastructure.Helpers;

public class SqlHelper
{
    private readonly DbContext _dbContext;

    public SqlHelper(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    //Fetch Single values
    public T ExecuteScalar<T>(string query, SqlParameter[]? parameters = null)
    {
        using var connection = new SqlConnection(_dbContext.ConnectionString);
        using var command = new SqlCommand(query, connection);

        if (parameters != null) command.Parameters.AddRange(parameters);

        connection.Open();
        object? result = command.ExecuteScalar();

        if (result == null || result == DBNull.Value)
            return default!;

        return (T)Convert.ChangeType(result, typeof(T));
    }

    //For Changing Data (Insert, Update, Delete)
    public int ExecuteNonQuery(string query, SqlParameter[]? parameters = null)
    {
        using var connection = new SqlConnection(_dbContext.ConnectionString);
        using var command = new SqlCommand(query, connection);

        if (parameters != null) command.Parameters.AddRange(parameters);

        connection.Open();
        return command.ExecuteNonQuery();
    }

    //Fetch Lists/rows
    public DataTable ExecuteQuery(string query, SqlParameter[]? parameters = null)
    {
        using var connection = new SqlConnection(_dbContext.ConnectionString);
        using var command = new SqlCommand(query, connection);

        if (parameters != null) command.Parameters.AddRange(parameters);

        using var adapter = new SqlDataAdapter(command);
        var dataTable = new DataTable();
        adapter.Fill(dataTable);
        return dataTable;
    }
}