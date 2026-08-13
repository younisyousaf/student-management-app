namespace StudentManagement.AI.RAG.Readers;

public sealed class TextKnowledgeDocumentReader
    : IKnowledgeDocumentReader
{
    public bool CanRead(string extension) =>
        extension is ".txt" or ".md";

    public Task<string> ReadAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        return File.ReadAllTextAsync(
            filePath,
            cancellationToken);
    }
}