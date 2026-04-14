namespace ShoppingCartService.Application.Contracts;

public sealed record AddCartItemRequest(
    Guid RestaurantId,
    string RestaurantName,
    string Currency,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    string? SpecialInstructions,
    string? RequestId = null);

public sealed record UpdateCartItemQuantityRequest(int Quantity, string? RequestId = null);

public sealed record DiscountAppliedRequest(decimal Amount, string? Reason, string? RequestId = null);
