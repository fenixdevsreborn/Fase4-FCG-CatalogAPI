using CatalogAPI.Domain.Interfaces;
using CatalogAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace CatalogAPI.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly CatalogDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RollbackAndThrowAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        // Clear tracked entities to remove from database
        _context.ChangeTracker.Clear();
        
        await RollbackAsync(cancellationToken);
        throw exception;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
