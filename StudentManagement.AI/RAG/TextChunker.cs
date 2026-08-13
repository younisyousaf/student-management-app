namespace StudentManagement.AI.RAG;

public sealed class TextChunker
{
    public IReadOnlyList<string> ChunkByParagraphs(
        string text,
        int maxCharacters = 800)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var paragraphs = text
            .Split(
                ["\r\n\r\n", "\n\n"],
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

        var chunks = new List<string>();
        var current = new List<string>();
        int currentLength = 0;

        foreach (var paragraph in paragraphs)
        {
            if (current.Count > 0 &&
                currentLength + paragraph.Length > maxCharacters)
            {
                chunks.Add(string.Join(
                    Environment.NewLine + Environment.NewLine,
                    current));

                current.Clear();
                currentLength = 0;
            }

            current.Add(paragraph);
            currentLength += paragraph.Length;
        }

        if (current.Count > 0)
        {
            chunks.Add(string.Join(
                Environment.NewLine + Environment.NewLine,
                current));
        }

        return chunks;
    }
    public IReadOnlyList<string> ChunkMarkdownSections(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var lines = text
            .Split(
                ['\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

        var chunks = new List<string>();
        var current = new List<string>();

        foreach (var line in lines)
        {
            if (line.StartsWith("## ") && current.Count > 0)
            {
                chunks.Add(string.Join(
                    Environment.NewLine,
                    current));

                current.Clear();
            }

            current.Add(line);
        }

        if (current.Count > 0)
        {
            chunks.Add(string.Join(
                Environment.NewLine,
                current));
        }

        return chunks;
    }

    public IReadOnlyList<string> ChunkBySize(
     string text,
     int maxCharacters = 300,
     int overlapCharacters = 50)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        if (maxCharacters <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCharacters));
        }

        if (overlapCharacters < 0 ||
            overlapCharacters >= maxCharacters)
        {
            throw new ArgumentOutOfRangeException(
                nameof(overlapCharacters),
                "Overlap must be smaller than the chunk size.");
        }

        string normalized =
            string.Join(
                " ",
                text.Split(
                    ['\r', '\n', '\t'],
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries));

        var chunks = new List<string>();

        int start = 0;

        while (start < normalized.Length)
        {
            int length =
                Math.Min(
                    maxCharacters,
                    normalized.Length - start);

            int end = start + length;

            if (end < normalized.Length)
            {
                int lastSpace =
                    normalized.LastIndexOf(
                        ' ',
                        end - 1,
                        length);

                if (lastSpace > start)
                {
                    end = lastSpace;
                }
            }

            string chunk =
                normalized[start..end].Trim();

            if (!string.IsNullOrWhiteSpace(chunk))
            {
                chunks.Add(chunk);
            }

            if (end >= normalized.Length)
            {
                break;
            }

            start =
                Math.Max(
                    end - overlapCharacters,
                    start + 1);

            while (
                start < normalized.Length &&
                normalized[start] == ' ')
            {
                start++;
            }
        }

        return chunks;
    }
}