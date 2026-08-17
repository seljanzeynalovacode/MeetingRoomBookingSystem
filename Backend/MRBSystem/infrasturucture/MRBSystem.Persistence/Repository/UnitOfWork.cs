using Microsoft.EntityFrameworkCore;
using MRBSystem.Domain.Repository;
using MRBSystem.Persistence.Data;
using System.Data;

namespace MRBSystem.Persistence.Repository;

public class UnitOfWork : IUnitOfWork
{
    private readonly MRBAppDbContext _context;

    public UnitOfWork(MRBAppDbContext context)
    {
        _context = context;
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> operation,
        IsolationLevel isolationLevel = IsolationLevel.Serializable)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await ExecutionStrategyExtensions.ExecuteAsync(strategy, async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(isolationLevel);

            try
            {
                var result = await operation();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }
}
