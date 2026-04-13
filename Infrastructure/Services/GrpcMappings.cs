using ShoppingCartService.Application.Contracts;
using ShoppingCartService.Infrastructure.Protos;

namespace ShoppingCartService.Infrastructure.Services;

public static class GrpcMappings
{
    public static CartReply ToReply(this CartDto cart)
    {
        var reply = new CartReply
        {
            Id = cart.Id.ToString(),
            CustomerId = cart.CustomerId.ToString(),
            RestaurantId = cart.RestaurantId.ToString(),
            RestaurantName = cart.RestaurantName,
            Currency = cart.Currency,
            Status = cart.Status,
            TotalItems = cart.TotalItems,
            DiscountAmount = decimal.ToDouble(cart.DiscountAmount),
            DiscountReason = cart.DiscountReason ?? string.Empty,
            TotalAmount = decimal.ToDouble(cart.TotalAmount),
            CreatedAtUtc = cart.CreatedAtUtc.ToString("O"),
            UpdatedAtUtc = cart.UpdatedAtUtc.ToString("O"),
            CheckedOutAtUtc = cart.CheckedOutAtUtc?.ToString("O") ?? string.Empty
        };

        reply.Items.AddRange(cart.Items.Select(ToReply));
        return reply;
    }

    private static CartItemReply ToReply(CartItemDto item)
    {
        return new CartItemReply
        {
            ProductId = item.ProductId.ToString(),
            ProductName = item.ProductName,
            Quantity = item.Quantity,
            UnitPrice = decimal.ToDouble(item.UnitPrice),
            TotalPrice = decimal.ToDouble(item.TotalPrice),
            SpecialInstructions = item.SpecialInstructions ?? string.Empty
        };
    }
}
