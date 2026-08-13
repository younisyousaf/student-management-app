using System.Text;
using UglyToad.PdfPig;

namespace StudentManagement.AI.RAG.Readers;

public sealed class PdfKnowledgeDocumentReader
    : IKnowledgeDocumentReader
{
    public bool CanRead(string extension) =>
        extension == ".pdf";

    public Task<string> ReadAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();

        using var document =
            PdfDocument.Open(filePath);

        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            string pageText = page.Text;

            if (!string.IsNullOrWhiteSpace(pageText))
            {
                builder.AppendLine(pageText);
                builder.AppendLine();
            }
        }

        return Task.FromResult(
            builder.ToString());
    }
}