using ElBruno.LocalEmbeddings;

namespace StudentManagement.AI.Embeddings;

public static class EmbeddingTestService
{
    public static async Task TestAsync()
    {
        await using var generator =
            await LocalEmbeddingGenerator.CreateAsync();

        var embedding =
            await generator.GenerateEmbeddingAsync(
                "Students must maintain attendance.");

        Console.WriteLine(
            $"Embedding dimensions: {embedding.Vector.Length}");

        Console.WriteLine(
            $"First 5 values: {string.Join(", ", embedding.Vector.Span[..5].ToArray())}");
    }


}