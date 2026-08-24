using System.ComponentModel;
using StudentManagement.AI.RAG.Models;

namespace StudentManagement.AI.Tools.Hosted;

public sealed class HostedKnowledgeTools
{
    private readonly ScopedToolExecutor _executor;

    public HostedKnowledgeTools(
        ScopedToolExecutor executor)
    {
        _executor = executor;
    }

    [Description(
        "Search institutional policies, handbook content, exam rules, " +
        "fee policies, attendance policies, and other institutional knowledge. " +
        "Do not use this for live SQL-backed application data.")]
    public Task<KnowledgeToolResult>
        SearchInstitutionalKnowledge(
            [Description(
                "The institutional policy or knowledge question.")]
            string query,

            CancellationToken cancellationToken = default)
    {
        return _executor.ExecuteAsync<
            KnowledgeTools,
            KnowledgeToolResult>(
                (tools, token) =>
                    tools.SearchInstitutionalKnowledge(
                        query,
                        token),
                cancellationToken);
    }
}
