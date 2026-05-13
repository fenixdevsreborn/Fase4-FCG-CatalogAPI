using CatalogAPI.Domain.Entities;
using CatalogAPI.Domain.Interfaces;
using CatalogAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CatalogAPI.Infrastructure.Repositories;

public class GameRepository : IGameRepository
{
    private readonly CatalogDbContext _context;

    public GameRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<List<Game>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .OrderBy(g => g.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Games.CountAsync(cancellationToken);
    }

    public async Task<List<Game>> SearchAsync(
        string searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedSearch = $"%{searchTerm.Trim()}%";

        return await _context.Games
            .Where(g =>
                EF.Functions.ILike(g.Name, normalizedSearch) ||
                EF.Functions.ILike(g.Description, normalizedSearch) ||
                EF.Functions.ILike(g.Genre, normalizedSearch) ||
                EF.Functions.ILike(g.Developer, normalizedSearch))
            .OrderBy(g => g.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Game>> GetAllForIndexingAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .OrderBy(g => g.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> SearchTotalCountAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var normalizedSearch = $"%{searchTerm.Trim()}%";

        return await _context.Games.CountAsync(
            g =>
                EF.Functions.ILike(g.Name, normalizedSearch) ||
                EF.Functions.ILike(g.Description, normalizedSearch) ||
                EF.Functions.ILike(g.Genre, normalizedSearch) ||
                EF.Functions.ILike(g.Developer, normalizedSearch),
            cancellationToken);
    }

    public async Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<Game?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<Game> AddAsync(Game game, CancellationToken cancellationToken = default)
    {
        await _context.Games.AddAsync(game, cancellationToken);
        return game;
    }

    public async Task UpdateAsync(Game game, CancellationToken cancellationToken = default)
    {
        _context.Games.Update(game);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var game = await _context.Games.FindAsync(new object[] { id }, cancellationToken);
        if (game != null)
        {
            _context.Games.Remove(game);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Games.AnyAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Games.AnyAsync(g => g.Name.ToLower() == name.ToLower(), cancellationToken);
    }
}
