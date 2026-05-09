namespace CatalogAPI.Domain.Interfaces;

public interface IOutbox
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
    Task SaveChangesAndFlushMessagesAsync(CancellationToken cancellationToken = default);
}
