namespace StudentManagement.Core.Exceptions;

public sealed class ApplicationDataUnavailableException : Exception
{
    public ApplicationDataUnavailableException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}
