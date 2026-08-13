namespace StudentManagement.AI.RAG.Readers;

public interface IKnowledgeDocumentReader
{
    bool CanRead(string extension);

    Task<string> ReadAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}