namespace StudentManagement.AI.RAG;

public sealed class KnowledgeStoreUnavailableException : Exception
{
    public KnowledgeStoreUnavailableException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}
