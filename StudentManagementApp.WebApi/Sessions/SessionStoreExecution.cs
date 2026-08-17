using Microsoft.Data.SqlClient;
using StudentManagement.AI.Sessions;
using StudentManagement.Infrastructure.Hybrid.Reliability;

namespace StudentManagementApp.WebApi.Sessions;

internal static class SessionStoreExecution
{
    public static async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation)
    {
        try
        {
            return await operation();
        }
        catch (SqlException ex)
            when (SqlFailureClassifier.IsAvailabilityFailure(ex))
        {
            throw new SessionStoreUnavailableException(
                "The Copilot session store is temporarily unavailable.",
                ex);
        }
    }

    public static async Task ExecuteAsync(
        Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (SqlException ex)
            when (SqlFailureClassifier.IsAvailabilityFailure(ex))
        {
            throw new SessionStoreUnavailableException(
                "The Copilot session store is temporarily unavailable.",
                ex);
        }
    }
}
