using ShoppingCartService.Application.Contracts;
using ShoppingCartService.Domain.Entities;

namespace ShoppingCartService.Application.Mapping;

public static class CartMappings
{
    public static CartDto ToDto(this ShoppingCart cart)
    {
        return new CartDto(
            cart.Id,
            cart.CustomerId,
            cart.RestaurantId,
            cart.RestaurantName,
            cart.Currency,
            cart.Status.ToString(),
            cart.TotalItems,
            cart.DiscountAmount,
            cart.DiscountReason,
            cart.TotalAmount,
            cart.CreatedAtUtc,
            cart.UpdatedAtUtc,
            cart.CheckedOutAtUtc,
            cart.Items
                .Select(item => new CartItemDto(
                    item.ProductId,
                    item.ProductName,
                    item.Quantity,
                    item.UnitPrice,
                    item.TotalPrice,
                    item.SpecialInstructions))
                .ToArray());
    }
}
