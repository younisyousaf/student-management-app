using Microsoft.Extensions.DependencyInjection;

namespace StudentManagement.AI.Tools.Hosted;

public sealed class ScopedToolExecutor
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ScopedToolExecutor(
        IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public TResult Execute<TTool, TResult>(
        Func<TTool, TResult> action)
        where TTool : notnull
    {
        using var scope =
            _scopeFactory.CreateScope();

        var tool =
            scope.ServiceProvider
                .GetRequiredService<TTool>();

        return action(tool);
    }

    public async Task<TResult> ExecuteAsync<TTool, TResult>(
        Func<TTool, CancellationToken, Task<TResult>> action,
        CancellationToken cancellationToken = default)
        where TTool : notnull
    {
        await using var scope =
            _scopeFactory.CreateAsyncScope();

        var tool =
            scope.ServiceProvider
                .GetRequiredService<TTool>();

        return await action(
            tool,
            cancellationToken);
    }
}
