namespace MRBSystem.Domain.Repository;

public interface IGenericRepository<T> where T : class, new()
{
    Task AddAsync(T entity);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(Guid id);
    Task UpdateAsync(T entity);
    IQueryable<T> GetQueryable();
}
