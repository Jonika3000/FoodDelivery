using ShoppingCartService.Domain.Entities;

namespace ShoppingCartService.Application.Abstractions;

public interface IShoppingCartEventStore
{
    Task AppendAsync(ShoppingCartEvent shoppingCartEvent, CancellationToken cancellationToken);

    Task<bool> HasBeenProcessedAsync(string requestId, CancellationToken cancellationToken);

    Task MarkAsProcessedAsync(string requestId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
