using Microsoft.Data.SqlClient;
using StudentManagement.Core.Exceptions;

namespace StudentManagement.Infrastructure.Hybrid.Reliability;

public static class DatabaseExecution
{
    public static T Execute<T>(Func<T> operation)
    {
        try
        {
            return operation();
        }
        catch (SqlException ex)
            when (SqlFailureClassifier.IsAvailabilityFailure(ex))
        {
            throw new ApplicationDataUnavailableException(
                "Application data is temporarily unavailable.",
                ex);
        }
    }

    public static void Execute(Action operation)
    {
        try
        {
            operation();
        }
        catch (SqlException ex)
            when (SqlFailureClassifier.IsAvailabilityFailure(ex))
        {
            throw new ApplicationDataUnavailableException(
                "Application data is temporarily unavailable.",
                ex);
        }
    }
}
