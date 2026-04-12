using ShoppingCartService.Domain.Entities;

namespace ShoppingCartService.Application.Abstractions;

public interface IShoppingCartEventStore
{
    Task AppendAsync(ShoppingCartEvent shoppingCartEvent, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
