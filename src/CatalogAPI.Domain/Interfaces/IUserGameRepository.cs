using CatalogAPI.Domain.Entities;

namespace CatalogAPI.Domain.Interfaces;

public interface IUserGameRepository
{
    Task<UserGame?> GetByUserAndGameAsync(Guid userId, Guid gameId, CancellationToken cancellationToken = default);
    Task<UserGame?> GetByUserAndGameWithGameAsync(Guid userId, Guid gameId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserGame>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserGame>> GetByUserWithGameAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetCountByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserGame userGame, CancellationToken cancellationToken = default);
    Task RemoveAsync(UserGame userGame, CancellationToken cancellationToken = default);
}
