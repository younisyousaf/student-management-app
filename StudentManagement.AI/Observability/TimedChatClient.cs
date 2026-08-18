using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace StudentManagement.AI.Observability;

public sealed class TimedChatClient : DelegatingChatClient
{
    private readonly ILogger<TimedChatClient> _logger;

    public TimedChatClient(
        IChatClient innerClient,
        ILogger<TimedChatClient> logger)
        : base(innerClient)
    {
        _logger = logger;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await base.GetResponseAsync(
                messages,
                options,
                cancellationToken);
        }
        finally
        {
            stopwatch.Stop();

            _logger.LogInformation(
                "LLM call finished in {ElapsedMilliseconds} ms.",
                stopwatch.ElapsedMilliseconds);
        }
    }
}