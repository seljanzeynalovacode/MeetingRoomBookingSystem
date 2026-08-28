using MRBSystem.Domain.Repository;
using System.Data;

namespace MRBSystem.Tests;

// SQLite Serializable-i tam deteklemir, ona gore testde real tranzaksiya acmadan
// birbasa operation-u icra edirik
public class FakeUnitOfWork : IUnitOfWork
{
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> operation,
        IsolationLevel isolationLevel = IsolationLevel.Serializable)
    {
        return await operation();
    }
}