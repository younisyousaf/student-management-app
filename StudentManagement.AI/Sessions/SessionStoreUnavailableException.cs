namespace StudentManagement.AI.Sessions;

public sealed class SessionStoreUnavailableException : Exception
{
    public SessionStoreUnavailableException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}
