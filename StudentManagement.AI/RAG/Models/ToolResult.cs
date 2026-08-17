namespace StudentManagement.AI.Models;

public sealed record ToolResult<T>(
    bool Success,
    bool Found,
    T? Data,
    string? Message);
