using Microsoft.EntityFrameworkCore;
using ShoppingCartService.Application.Abstractions;
using ShoppingCartService.Domain.Entities;

namespace ShoppingCartService.Infrastructure.Persistence;

public sealed class OutboxRepository(ShoppingCartDbContext dbContext) : IOutboxRepository
{
    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        await dbContext.OutboxMessages.AddAsync(message, cancellationToken);
    }

    public Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize, CancellationToken cancellationToken)
    {
        return dbContext.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null && m.RetryCount < 3 && !m.IsDeadLetter)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
