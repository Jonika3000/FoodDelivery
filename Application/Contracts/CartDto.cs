namespace ShoppingCartService.Application.Contracts;

public sealed record CartItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    string? SpecialInstructions);

public sealed record CartDto(
    Guid Id,
    Guid CustomerId,
    Guid RestaurantId,
    string RestaurantName,
    string Currency,
    string Status,
    int TotalItems,
    decimal DiscountAmount,
    string? DiscountReason,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? CheckedOutAtUtc,
    IReadOnlyCollection<CartItemDto> Items);
