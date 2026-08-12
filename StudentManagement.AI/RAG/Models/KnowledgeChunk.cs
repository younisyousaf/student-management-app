namespace StudentManagement.AI.RAG.Models;

public sealed class KnowledgeChunk
{
    public Guid Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public string DocumentName { get; set; } = string.Empty;

    public string? Section { get; set; }

    public int ChunkIndex { get; set; }

    public ReadOnlyMemory<float> Vector { get; set; }
}