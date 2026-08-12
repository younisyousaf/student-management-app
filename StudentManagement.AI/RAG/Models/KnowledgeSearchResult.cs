namespace StudentManagement.AI.RAG.Models;

public sealed record KnowledgeSearchResult(
    string Text,
    string DocumentName,
    string? Section,
    int ChunkIndex,
    float Score);