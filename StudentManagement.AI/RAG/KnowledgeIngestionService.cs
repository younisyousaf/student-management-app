using StudentManagement.AI.RAG.Readers;

namespace StudentManagement.AI.RAG;

public sealed class KnowledgeIngestionService
{
    private readonly QdrantKnowledgeStore _knowledgeStore;
    private readonly IEnumerable<IKnowledgeDocumentReader> _readers;

    public KnowledgeIngestionService(
        QdrantKnowledgeStore knowledgeStore,
        IEnumerable<IKnowledgeDocumentReader> readers)
    {
        _knowledgeStore = knowledgeStore;
        _readers = readers;
    }

    public async Task IngestDocumentAsync(
     string filePath,
     CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "File path is required.",
                nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "Knowledge document was not found.",
                filePath);
        }

        // Get the file extension, e.g. .md, .txt, .pdf
        string extension =
            Path.GetExtension(filePath).ToLowerInvariant();

        // Find the reader that supports this file type
        var reader =
            _readers.FirstOrDefault(
                r => r.CanRead(extension));

        if (reader is null)
        {
            throw new NotSupportedException(
                $"File type '{extension}' is not supported.");
        }

        // Let the appropriate reader extract the text
        string text =
            await reader.ReadAsync(
                filePath,
                cancellationToken);
        var chunker = new TextChunker();

        IReadOnlyList<string> chunks =
            extension switch
            {
                ".md" => chunker.ChunkMarkdownSections(text),

                ".txt" => chunker.ChunkByParagraphs(
                    text,
                    maxCharacters: 800),

                ".pdf" => chunker.ChunkBySize(
                    text,
                    maxCharacters: 300,
                    overlapCharacters: 50
                    ),

                _ => throw new NotSupportedException(
                    $"File type '{extension}' is not supported.")
            };
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException(
                "Knowledge document is empty or no readable text was found.");
        }

        string documentName =
           Path.GetFileName(filePath);
       
        string documentId =
            CreateDocumentId(filePath);

        await _knowledgeStore.IngestDocumentAsync(
            documentId,
            documentName,
            chunks,
            cancellationToken: cancellationToken);
    }

    private static string CreateDocumentId(string filePath)
    {
        string fullPath =
            Path.GetFullPath(filePath)
                .ToLowerInvariant();

        byte[] bytes =
            System.Text.Encoding.UTF8.GetBytes(fullPath);

        byte[] hash =
            System.Security.Cryptography.SHA256.HashData(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}