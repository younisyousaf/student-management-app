using ElBruno.LocalEmbeddings;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using StudentManagement.AI.RAG.Models;

namespace StudentManagement.AI.RAG;

public sealed class QdrantKnowledgeStore
{
    private const string CollectionName = "student_management_knowledge";

    private const ulong VectorSize = 384;

    private readonly QdrantClient _client;

    public QdrantKnowledgeStore()
    {
        _client = new QdrantClient(
            host: "localhost",
            port: 6334);
    }

    public async Task EnsureCollectionExistsAsync(CancellationToken cancellationToken = default)
    {
        bool exists =
            await _client.CollectionExistsAsync(
                CollectionName,
                cancellationToken);

        if (exists)
        {
            return;
        }

        await _client.CreateCollectionAsync(
            CollectionName,
            new VectorParams
            {
                Size = VectorSize,
                Distance = Distance.Cosine
            },
            cancellationToken: cancellationToken);
    }

    public async Task UpsertChunkAsync(KnowledgeChunk chunk, CancellationToken cancellationToken = default)
    {
        var point = new PointStruct
        {
            Id = new PointId
            {
                Uuid = chunk.Id.ToString()
            },

            Vectors = chunk.Vector.ToArray()
        };

        point.Payload["text"] = chunk.Text;
        point.Payload["documentName"] = chunk.DocumentName;
        point.Payload["section"] = chunk.Section ?? string.Empty;
        point.Payload["chunkIndex"] = chunk.ChunkIndex;

        await _client.UpsertAsync(
            CollectionName,
            [point],
            cancellationToken: cancellationToken);
    }

    public async Task TestSearchAsync(
    CancellationToken cancellationToken = default)
    {
        var results = await SearchAsync(
            "Can a student take the final exam if they miss too many classes?",
            limit: 3,
            cancellationToken: cancellationToken);

        foreach (var result in results)
        {
            Console.WriteLine($"Score: {result.Score}");
            Console.WriteLine($"Document: {result.DocumentName}");
            Console.WriteLine($"Section: {result.Section}");
            Console.WriteLine($"Text: {result.Text}");
            Console.WriteLine();
        }
    }

    public async Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
    string query,
    int limit = 3,
    float minimumScore = 0.50f,
    CancellationToken cancellationToken = default)
    {
        await using var generator =
            await LocalEmbeddingGenerator.CreateAsync();

        var queryEmbedding =
            await generator.GenerateEmbeddingAsync(
                query,
                cancellationToken: cancellationToken);

        var results =
        await _client.QueryAsync(
            collectionName: CollectionName,
            query: new Query(queryEmbedding.Vector.ToArray()),
            limit: (ulong)limit,
            payloadSelector: true,
            cancellationToken: cancellationToken);

        Console.WriteLine($"Qdrant returned {results.Count} results.");

        return results
    .Where(result => result.Score >= minimumScore)
    .Select(result =>
        new KnowledgeSearchResult(
            Text: result.Payload.TryGetValue("text", out var text)
                ? text.StringValue
                : string.Empty,

            DocumentName: result.Payload.TryGetValue(
                "documentName",
                out var documentName)
                ? documentName.StringValue
                : string.Empty,

            Section: result.Payload.TryGetValue(
                "section",
                out var section)
                ? section.StringValue
                : null,

            ChunkIndex: result.Payload.TryGetValue(
                "chunkIndex",
                out var chunkIndex)
                ? (int)chunkIndex.IntegerValue
                : 0,

            Score: result.Score))
    .ToList();
    }

    public async Task IngestDocumentAsync(
    string documentName,
    string text,
    string? section = null,
    int maxCharacters = 300,
    CancellationToken cancellationToken = default)
    {
        var chunker = new TextChunker();

        IReadOnlyList<string> chunks =
            //chunker.ChunkByParagraphs(
            //    text,
            //    maxCharacters);
            chunker.ChunkMarkdownSections(text);

        await using var generator =
            await LocalEmbeddingGenerator.CreateAsync();

        var points = new List<PointStruct>();

        for (int i = 0; i < chunks.Count; i++)
        {
            string chunkText = chunks[i];

            var embedding =
                await generator.GenerateEmbeddingAsync(
                    chunkText,
                    cancellationToken: cancellationToken);

            var point = new PointStruct
            {
                Id = new PointId
                {
                    Uuid = Guid.NewGuid().ToString()
                },

                Vectors = embedding.Vector.ToArray()
            };

            point.Payload["text"] = chunkText;
            point.Payload["documentName"] = documentName;
            point.Payload["section"] = section ?? string.Empty;
            point.Payload["chunkIndex"] = i;

            points.Add(point);
        }

        if (points.Count == 0)
            return;

        await _client.UpsertAsync(
            CollectionName,
            points,
            cancellationToken: cancellationToken);
    }
}