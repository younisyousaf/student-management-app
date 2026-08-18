using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace StudentManagement.AI.Observability;

public sealed class TimedAIFunction : DelegatingAIFunction
{
    private readonly ILogger _logger;

    public TimedAIFunction(
        AIFunction innerFunction,
        ILogger logger)
        : base(innerFunction)
    {
        _logger = logger;
    }

    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await base.InvokeCoreAsync(
                arguments,
                cancellationToken);
        }
        finally
        {
            stopwatch.Stop();

            _logger.LogInformation(
                "AI tool {ToolName} finished in {ElapsedMilliseconds} ms.",
                Name,
                stopwatch.ElapsedMilliseconds);
        }
    }
}