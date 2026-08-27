using ElBruno.LocalEmbeddings;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using StudentManagement.AI.RAG.Models;
using System.Diagnostics;

namespace StudentManagement.AI.RAG;

public sealed class QdrantKnowledgeStore
{
    private const string CollectionName = "student_management_knowledge";

    private const ulong VectorSize = 384;

    private readonly QdrantClient _client;
    private readonly ILogger<QdrantKnowledgeStore> _logger;

    public QdrantKnowledgeStore(ILogger<QdrantKnowledgeStore> logger)
    {
        _client = new QdrantClient(
            host: "localhost",
            port: 6334);
        _logger = logger;
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
        var totalStopwatch = Stopwatch.StartNew();

        var generatorStopwatch = Stopwatch.StartNew();

        await using var generator =
            await LocalEmbeddingGenerator.CreateAsync();

        generatorStopwatch.Stop();

        _logger.LogInformation(
            "Local embedding generator initialized in {ElapsedMilliseconds} ms.",
            generatorStopwatch.ElapsedMilliseconds);


        var embeddingStopwatch = Stopwatch.StartNew();

        var queryEmbedding =
            await generator.GenerateEmbeddingAsync(
                query,
                cancellationToken: cancellationToken);

        embeddingStopwatch.Stop();

        _logger.LogInformation(
            "Query embedding generated in {ElapsedMilliseconds} ms.",
            embeddingStopwatch.ElapsedMilliseconds);


        try
        {
            var qdrantStopwatch = Stopwatch.StartNew();

            var results =
                await _client.QueryAsync(
                    collectionName: CollectionName,
                    query: new Query(
                        queryEmbedding.Vector.ToArray()),
                    limit: (ulong)limit,
                    payloadSelector: true,
                    cancellationToken: cancellationToken);

            qdrantStopwatch.Stop();

            _logger.LogInformation(
                "Qdrant query returned {ResultCount} results in {ElapsedMilliseconds} ms.",
                results.Count,
                qdrantStopwatch.ElapsedMilliseconds);


            var mappingStopwatch = Stopwatch.StartNew();

            var mappedResults = results
                .Where(result =>
                    result.Score >= minimumScore)
                .Select(result =>
                    new KnowledgeSearchResult(
                        Text: result.Payload.TryGetValue(
                            "text",
                            out var text)
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

            mappingStopwatch.Stop();
            totalStopwatch.Stop();

            _logger.LogInformation(
                "Qdrant results filtered and mapped in {ElapsedMilliseconds} ms.",
                mappingStopwatch.ElapsedMilliseconds);

            _logger.LogInformation(
                "Knowledge search finished in {ElapsedMilliseconds} ms.",
                totalStopwatch.ElapsedMilliseconds);

            return mappedResults;
        }
        catch (RpcException ex)
        when (
        ex.StatusCode == StatusCode.Cancelled &&
        cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(
                "The institutional knowledge search was cancelled.",
                ex,
                cancellationToken);
        }
        catch (RpcException ex)
            when (ex.StatusCode is
                StatusCode.Unavailable or
                StatusCode.DeadlineExceeded)
        {
            throw new KnowledgeStoreUnavailableException(
                "The institutional knowledge store is temporarily unavailable.",
                ex);
        }
        catch (RpcException ex)
            when (ex.StatusCode is
                StatusCode.Unavailable or
                StatusCode.DeadlineExceeded)
        {
            throw new KnowledgeStoreUnavailableException(
                "The institutional knowledge store is temporarily unavailable.",
                ex);
        }
    }

    public async Task IngestDocumentAsync(
    string documentId,
    string documentName,
    IReadOnlyList<string> chunks,
    string? section = null,
    CancellationToken cancellationToken = default)
    {
        await EnsureCollectionExistsAsync(cancellationToken);

        if (chunks.Count == 0)
        {
            throw new InvalidOperationException(
                "The document did not produce any knowledge chunks.");
        }

        await DeleteDocumentAsync(
            documentId,
            cancellationToken);

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
                    Uuid = CreateChunkId(
                        documentId,
                        i).ToString()
                },

                Vectors = embedding.Vector.ToArray()
            };

            point.Payload["documentId"] = documentId;
            point.Payload["text"] = chunkText;
            point.Payload["documentName"] = documentName;
            point.Payload["section"] = section ?? string.Empty;
            point.Payload["chunkIndex"] = i;

            points.Add(point);
        }

        await _client.UpsertAsync(
            CollectionName,
            points,
            cancellationToken: cancellationToken);
    }

    public async Task DeleteDocumentAsync(
    string documentId,
    CancellationToken cancellationToken = default)
    {
        var filter = new Filter();

        filter.Must.Add(
            new Condition
            {
                Field = new FieldCondition
                {
                    Key = "documentId",
                    Match = new Match
                    {
                        Keyword = documentId
                    }
                }
            });

        await _client.DeleteAsync(
            collectionName: CollectionName,
            filter: filter,
            cancellationToken: cancellationToken);
    }


    private static Guid CreateChunkId(
    string documentId,
    int chunkIndex)
    {
        string value =
            $"{documentId}:{chunkIndex}";

        byte[] hash =
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(value));

        Span<byte> guidBytes = stackalloc byte[16];

        hash.AsSpan(0, 16).CopyTo(guidBytes);

        return new Guid(guidBytes);
    }
}