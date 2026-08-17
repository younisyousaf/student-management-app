using System.ClientModel;

namespace StudentManagement.AI.Reliability;

public static class AIProviderFailureClassifier
{
    public static bool IsTemporaryFailure(
        ClientResultException exception)
    {
        return exception.Status is
            408 or     // Request timeout
            429 or     // Rate limit / provider capacity
            500 or     // Provider internal error
            502 or     // Bad gateway
            503 or     // Service unavailable
            504;       // Gateway timeout
    }

    public static bool IsAuthenticationFailure(
        ClientResultException exception)
    {
        return exception.Status is
            401 or     // Invalid/missing credentials
            403;       // Credentials not permitted
    }
}
