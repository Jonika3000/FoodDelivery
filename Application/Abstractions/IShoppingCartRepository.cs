using ShoppingCartService.Domain.Entities;

namespace ShoppingCartService.Application.Abstractions;

public interface IShoppingCartRepository
{
    Task<ShoppingCart?> GetActiveByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken);

    Task AddAsync(ShoppingCart cart, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
