using ShoppingCartService.Application.Contracts;

namespace ShoppingCartService.Application.Abstractions;

public interface IShoppingCartService
{
    Task<CartDto?> GetActiveCartAsync(Guid customerId, CancellationToken cancellationToken);

    Task<CartDto> AddItemAsync(Guid customerId, AddCartItemRequest request, CancellationToken cancellationToken);

    Task<CartDto> UpdateItemQuantityAsync(Guid customerId, Guid productId, int quantity, CancellationToken cancellationToken);

    Task DeleteItemAsync(Guid customerId, Guid productId, CancellationToken cancellationToken);

    Task ClearAsync(Guid customerId, CancellationToken cancellationToken);

    Task<CartDto> DiscountAppliedAsync(Guid customerId, DiscountAppliedRequest request, CancellationToken cancellationToken);

    Task<CartDto> CheckoutAsync(Guid customerId, CancellationToken cancellationToken);
}
