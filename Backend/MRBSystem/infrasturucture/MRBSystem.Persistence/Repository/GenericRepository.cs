using Microsoft.EntityFrameworkCore;
using MRBSystem.Application.Exceptions;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Repository;
using MRBSystem.Persistence.Data;

namespace MRBSystem.Persistence.Repository;

public class GenericRepository<T> : IGenericRepository<T>where T : BaseEntity, new()
{
    private readonly MRBAppDbContext _context;
    private readonly DbSet<T> _dbSet;

    public GenericRepository(MRBAppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity is null)
        {
            throw new NotFoundException($"{typeof(T).Name} with id {id} not found.");
        }

        _dbSet.Remove(entity);

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.AsNoTracking().ToListAsync();
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task UpdateAsync(T entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;

        _dbSet.Update(entity);

        await _context.SaveChangesAsync();
    }
    public IQueryable<T> GetQueryable()
    {
        return _dbSet.AsNoTracking();
    }
}