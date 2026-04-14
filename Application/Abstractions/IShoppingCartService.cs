using ShoppingCartService.Application.Contracts;

namespace ShoppingCartService.Application.Abstractions;

public interface IShoppingCartService
{
    Task<CartDto?> GetActiveCartAsync(Guid customerId, CancellationToken cancellationToken);

    Task<CartDto> AddItemAsync(Guid customerId, AddCartItemRequest request, CancellationToken cancellationToken);

    Task<CartDto> UpdateItemQuantityAsync(Guid customerId, Guid productId, UpdateCartItemQuantityRequest request, CancellationToken cancellationToken);

    Task DeleteItemAsync(Guid customerId, Guid productId, string? requestId, CancellationToken cancellationToken);

    Task ClearAsync(Guid customerId, string? requestId, CancellationToken cancellationToken);

    Task<CartDto> DiscountAppliedAsync(Guid customerId, DiscountAppliedRequest request, CancellationToken cancellationToken);

    Task<CartDto> CheckoutAsync(Guid customerId, string? requestId, CancellationToken cancellationToken);
}
