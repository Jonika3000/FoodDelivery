using Microsoft.EntityFrameworkCore;
using ShoppingCartService.Application.Abstractions;
using ShoppingCartService.Domain.Entities;
using ShoppingCartService.Domain.Enums;

namespace ShoppingCartService.Infrastructure.Persistence;

public sealed class ShoppingCartRepository(ShoppingCartDbContext dbContext) : IShoppingCartRepository
{
    public Task<ShoppingCart?> GetActiveByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken)
    {
        return dbContext.ShoppingCarts
            .Include(cart => cart.Items)
            .SingleOrDefaultAsync(
                cart => cart.CustomerId == customerId && cart.Status == CartStatus.Active,
                cancellationToken);
    }

    public async Task AddAsync(ShoppingCart cart, CancellationToken cancellationToken)
    {
        await dbContext.ShoppingCarts.AddAsync(cart, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
