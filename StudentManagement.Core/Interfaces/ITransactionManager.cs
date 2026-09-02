namespace StudentManagement.Core.Interfaces;

public interface ITransactionManager
{
    void Execute(Action operation);
}