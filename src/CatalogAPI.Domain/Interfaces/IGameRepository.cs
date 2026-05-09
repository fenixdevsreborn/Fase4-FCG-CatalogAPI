using CatalogAPI.Domain.Entities;

namespace CatalogAPI.Domain.Interfaces;

public interface IGameRepository
{
    Task<List<Game>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
    Task<List<Game>> SearchAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<int> SearchTotalCountAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Game?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Game> AddAsync(Game game, CancellationToken cancellationToken = default);
    Task UpdateAsync(Game game, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
}
