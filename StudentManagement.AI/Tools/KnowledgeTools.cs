using Grpc.Core;
using StudentManagement.AI.RAG;
using StudentManagement.AI.RAG.Models;
using System.ComponentModel;

namespace StudentManagement.AI.Tools;

public sealed class KnowledgeTools
{
    private readonly QdrantKnowledgeStore _knowledgeStore;

    public KnowledgeTools(QdrantKnowledgeStore knowledgeStore)
    {
        _knowledgeStore = knowledgeStore;
    }

    [Description(
        "Search institutional policies, handbook content, exam rules, fee policies, " +
        "attendance policies, and other unstructured student-management knowledge. " +
        "Use this for policy or institutional-rule questions, not for live student, " +
        "course, attendance, enrollment, or fee records stored in SQL Server.")]
    public async Task<KnowledgeToolResult> SearchInstitutionalKnowledge(
    [Description("The policy or institutional knowledge question to search for.")]
    string query,
    CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await _knowledgeStore.SearchAsync(
                query,
                limit: 3,
                minimumScore: 0.50f,
                cancellationToken: cancellationToken);

            if (results.Count == 0)
            {
                return new KnowledgeToolResult(
                    Success: true,
                    Found: false,
                    Results: [],
                    Message:
                        "The institutional knowledge search completed successfully, " +
                        "but no sufficiently relevant policy was found.");
            }

            return new KnowledgeToolResult(
                Success: true,
                Found: true,
                Results: results,
                Message: null);
        }
        catch (KnowledgeStoreUnavailableException)
        {
            return new KnowledgeToolResult(
                Success: false,
                Found: false,
                Results: [],
                Message:
                    "Institutional knowledge is temporarily unavailable.");
        }
    }
}