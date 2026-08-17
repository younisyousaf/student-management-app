using Microsoft.Data.SqlClient;

namespace StudentManagement.Infrastructure.Hybrid.Reliability;

public static class SqlFailureClassifier
{
    public static bool IsAvailabilityFailure(
        SqlException exception)
    {
        return exception.Number is
            -2 or       // Command timeout
            -1 or       // Connection/instance unavailable
            26 or       // Error locating SQL Server/instance
            53 or       // SQL Server/network path unavailable
            64 or       // Network connection lost
            233 or      // Connection initialization failure
            10053 or    // Connection aborted
            10054 or    // Connection reset
            10060;      // Connection timeout
    }
}
