using ShoppingCartService.Application.Abstractions;
using ShoppingCartService.Domain.Entities;

namespace ShoppingCartService.Infrastructure.Persistence;

public sealed class ShoppingCartEventStore(EventStoreDbContext dbContext) : IShoppingCartEventStore
{
    public async Task AppendAsync(ShoppingCartEvent shoppingCartEvent, CancellationToken cancellationToken)
    {
        await dbContext.ShoppingCartEvents.AddAsync(shoppingCartEvent, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
