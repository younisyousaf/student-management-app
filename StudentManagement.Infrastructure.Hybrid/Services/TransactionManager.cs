using StudentManagement.Core.Interfaces;

namespace StudentManagement.Infrastructure.Hybrid.Services;

public sealed class TransactionManager(
    HybridDbContext context) : ITransactionManager
{
    public void Execute(Action operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        using var transaction =
            context.Database.BeginTransaction();

        try
        {
            operation();
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}