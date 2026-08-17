using System.Data;

namespace MRBSystem.Domain.Repository;

public interface IUnitOfWork
{
    Task<TResult> ExecuteInTransactionAsync<TResult>(
       Func<Task<TResult>> operation,
       IsolationLevel isolationLevel = IsolationLevel.Serializable);
}
