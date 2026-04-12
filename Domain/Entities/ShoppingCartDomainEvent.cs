namespace ShoppingCartService.Domain.Entities;

public interface IShoppingCartDomainEvent
{
    Guid CartId { get; }
    DateTime OccurredAtUtc { get; }
}

public sealed record ItemAdded(
    Guid CartId,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    string? SpecialInstructions,
    DateTime OccurredAtUtc) : IShoppingCartDomainEvent;

public sealed record ItemRemoved(
    Guid CartId,
    Guid ProductId,
    DateTime OccurredAtUtc) : IShoppingCartDomainEvent;

public sealed record DiscountApplied(
    Guid CartId,
    decimal Percentage,
    string? Reason,
    DateTime OccurredAtUtc) : IShoppingCartDomainEvent;

public sealed record CartCheckedOut(
    Guid CartId,
    DateTime OccurredAtUtc) : IShoppingCartDomainEvent;
