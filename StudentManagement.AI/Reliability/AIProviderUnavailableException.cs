namespace StudentManagement.AI.Reliability;

public sealed class AIProviderUnavailableException : Exception
{
    public AIProviderUnavailableException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}
