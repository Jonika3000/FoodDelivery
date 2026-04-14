using Microsoft.EntityFrameworkCore;
using ShoppingCartService.Application.Abstractions;
using ShoppingCartService.Domain.Entities;

namespace ShoppingCartService.Infrastructure.Persistence;

public sealed class ShoppingCartEventStore(EventStoreDbContext dbContext) : IShoppingCartEventStore
{
    public async Task AppendAsync(ShoppingCartEvent shoppingCartEvent, CancellationToken cancellationToken)
    {
        await dbContext.ShoppingCartEvents.AddAsync(shoppingCartEvent, cancellationToken);
    }

    public async Task<bool> HasBeenProcessedAsync(string requestId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestId)) return false;
        return await dbContext.ProcessedRequests.AnyAsync(r => r.RequestId == requestId, cancellationToken);
    }

    public async Task MarkAsProcessedAsync(string requestId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestId)) return;
        await dbContext.ProcessedRequests.AddAsync(new ProcessedRequest(requestId), cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
