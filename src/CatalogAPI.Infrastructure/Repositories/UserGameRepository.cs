using CatalogAPI.Domain.Entities;
using CatalogAPI.Domain.Interfaces;
using CatalogAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CatalogAPI.Infrastructure.Repositories;

public class UserGameRepository : IUserGameRepository
{
    private readonly CatalogDbContext _context;

    public UserGameRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<UserGame?> GetByUserAndGameAsync(Guid userId, Guid gameId, CancellationToken cancellationToken = default)
    {
        return await _context.UserGames
            .AsNoTracking()
            .FirstOrDefaultAsync(ug => ug.UserId == userId && ug.GameId == gameId, cancellationToken);
    }

    public async Task<UserGame?> GetByUserAndGameWithGameAsync(Guid userId, Guid gameId, CancellationToken cancellationToken = default)
    {
        return await _context.UserGames
            .AsNoTracking()
            .Include(ug => ug.Game)
            .FirstOrDefaultAsync(ug => ug.UserId == userId && ug.GameId == gameId, cancellationToken);
    }

    public async Task<IEnumerable<UserGame>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserGames
            .AsNoTracking()
            .Where(ug => ug.UserId == userId)
            .OrderByDescending(ug => ug.PurchaseDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UserGame>> GetByUserWithGameAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserGames
            .AsNoTracking()
            .Include(ug => ug.Game)
            .Where(ug => ug.UserId == userId)
            .OrderByDescending(ug => ug.PurchaseDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserGames
            .AsNoTracking()
            .CountAsync(ug => ug.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(UserGame userGame, CancellationToken cancellationToken = default)
    {
        await _context.UserGames.AddAsync(userGame, cancellationToken);
    }

    public Task RemoveAsync(UserGame userGame, CancellationToken cancellationToken = default)
    {
        _context.UserGames.Remove(userGame);
        return Task.CompletedTask;
    }
}
