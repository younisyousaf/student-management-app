namespace StudentManagement.AI.RAG.Models;

public sealed record KnowledgeToolResult(
    bool Success,
    bool Found,
    IReadOnlyList<KnowledgeSearchResult> Results,
    string? Message);
