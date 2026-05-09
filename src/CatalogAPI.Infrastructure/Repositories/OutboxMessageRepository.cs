using CatalogAPI.Domain.Entities;
using CatalogAPI.Domain.Interfaces;
using CatalogAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CatalogAPI.Infrastructure.Repositories;

public class OutboxMessageRepository : IOutboxMessageRepository
{
  private readonly CatalogDbContext _context;

  public OutboxMessageRepository(CatalogDbContext context)
  {
    _context = context;
  }

  public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
  {
    await _context.OutboxMessages.AddAsync(message, cancellationToken);
  }

  public async Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
  {
    return await _context.OutboxMessages
        .Where(m => m.ProcessedAt == null)
        .OrderBy(m => m.CreatedAt)
        .Take(batchSize)
        .ToListAsync(cancellationToken);
  }

  public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
  {
    var message = await _context.OutboxMessages.FindAsync(new object[] { messageId }, cancellationToken);
    if (message != null)
    {
      message.ProcessedAt = DateTimeOffset.UtcNow;
    }
  }

  public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
  {
    await _context.SaveChangesAsync(cancellationToken);
  }
}
