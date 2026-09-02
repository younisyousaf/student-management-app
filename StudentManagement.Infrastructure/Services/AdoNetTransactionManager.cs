using StudentManagement.Core.Interfaces;
using System.Transactions;

namespace StudentManagement.Infrastructure.Services;

public sealed class AdoNetTransactionManager : ITransactionManager
{
    public void Execute(Action operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        using var scope = new TransactionScope(
            TransactionScopeOption.Required,
            TransactionScopeAsyncFlowOption.Enabled);

        operation();

        scope.Complete();
    }
}
