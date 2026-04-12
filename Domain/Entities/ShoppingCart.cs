using ShoppingCartService.Domain.Abstractions;
using ShoppingCartService.Domain.Enums;
using ShoppingCartService.Domain.Exceptions;

namespace ShoppingCartService.Domain.Entities;

public class ShoppingCart : IAggregateRoot
{
    private readonly List<CartItem> _items = [];
    private readonly List<IShoppingCartDomainEvent> _uncommittedEvents = [];

    private ShoppingCart()
    {
        Status = CartStatus.Active;
    }

    public ShoppingCart(Guid customerId, Guid restaurantId, string restaurantName, string currency)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException("CustomerId is required.");
        }

        if (restaurantId == Guid.Empty)
        {
            throw new DomainException("RestaurantId is required.");
        }

        if (string.IsNullOrWhiteSpace(restaurantName))
        {
            throw new DomainException("Restaurant name is required.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainException("Currency is required.");
        }

        Id = Guid.NewGuid();
        CustomerId = customerId;
        RestaurantId = restaurantId;
        RestaurantName = restaurantName.Trim();
        Currency = currency.Trim().ToUpperInvariant();
        Status = CartStatus.Active;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid RestaurantId { get; private set; }

    public string RestaurantName { get; private set; } = string.Empty;

    public string Currency { get; private set; } = string.Empty;

    public CartStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime? CheckedOutAtUtc { get; private set; }

    public decimal DiscountAmount { get; private set; }

    public string? DiscountReason { get; private set; }

    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    public IReadOnlyCollection<IShoppingCartDomainEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();

    public decimal SubtotalAmount => decimal.Round(_items.Sum(item => item.TotalPrice), 2, MidpointRounding.AwayFromZero);

    public decimal TotalAmount => decimal.Round(SubtotalAmount - DiscountAmount, 2, MidpointRounding.AwayFromZero);

    public int TotalItems => _items.Sum(item => item.Quantity);

    public void AddItem(Guid productId, string productName, int quantity, decimal unitPrice, string? specialInstructions)
    {
        EnsureActive();
        EnsureValidQuantity(quantity);

        Raise(new ItemAdded(
            Id,
            productId,
            productName,
            quantity,
            unitPrice,
            specialInstructions,
            DateTime.UtcNow));
    }

    public void ChangeItemQuantity(Guid productId, int quantity)
    {
        EnsureActive();
        EnsureValidQuantity(quantity);

        var item = FindItem(productId);
        var delta = quantity - item.Quantity;
        if (delta == 0)
        {
            return;
        }

        if (delta < 0)
        {
            Raise(new ItemRemoved(Id, productId, DateTime.UtcNow));
            if (quantity > 0)
            {
                Raise(new ItemAdded(
                    Id,
                    item.ProductId,
                    item.ProductName,
                    quantity,
                    item.UnitPrice,
                    item.SpecialInstructions,
                    DateTime.UtcNow));
            }

            return;
        }

        Raise(new ItemAdded(
            Id,
            item.ProductId,
            item.ProductName,
            delta,
            item.UnitPrice,
            item.SpecialInstructions,
            DateTime.UtcNow));
    }

    public void RemoveItem(Guid productId)
    {
        EnsureActive();
        _ = FindItem(productId);
        Raise(new ItemRemoved(Id, productId, DateTime.UtcNow));
    }

    public void Clear()
    {
        EnsureActive();

        foreach (var item in _items.ToList())
        {
            Raise(new ItemRemoved(Id, item.ProductId, DateTime.UtcNow));
        }
    }

    public void AssignRestaurant(Guid restaurantId, string restaurantName, string currency)
    {
        EnsureActive();

        if (_items.Count > 0 && RestaurantId != restaurantId)
        {
            throw new DomainException("Food delivery cart can contain items from only one restaurant.");
        }

        RestaurantId = restaurantId;
        RestaurantName = restaurantName.Trim();
        Currency = currency.Trim().ToUpperInvariant();

        ResetDiscount();
        Touch();
    }

    public void DiscountApplied(decimal amount, string? reason)
    {
        ApplyDiscount(amount, reason);
    }

    public void ApplyDiscount(decimal percentage, string? reason)
    {
        EnsureActive();

        if (_items.Count == 0)
        {
            throw new DomainException("Cannot apply discount to an empty cart.");
        }

        if (percentage <= 0)
        {
            throw new DomainException("Discount percentage must be greater than zero.");
        }

        if (percentage > 50m)
        {
            throw new DomainException("Discount percentage cannot exceed 50%.");
        }

        Raise(new DiscountApplied(Id, percentage, reason, DateTime.UtcNow));
    }

    public void Checkout()
    {
        EnsureActive();

        if (_items.Count == 0)
        {
            throw new DomainException("Cannot checkout an empty cart.");
        }

        Raise(new CartCheckedOut(Id, DateTime.UtcNow));
    }

    public static ShoppingCart FromHistory(
        Guid customerId,
        Guid restaurantId,
        string restaurantName,
        string currency,
        IEnumerable<IShoppingCartDomainEvent> history)
    {
        var cart = new ShoppingCart(customerId, restaurantId, restaurantName, currency);
        cart._uncommittedEvents.Clear();

        foreach (var domainEvent in history.OrderBy(e => e.OccurredAtUtc))
        {
            cart.Apply(domainEvent);
        }

        cart._uncommittedEvents.Clear();
        return cart;
    }

    public void ClearUncommittedEvents()
    {
        _uncommittedEvents.Clear();
    }

    public void Apply(IShoppingCartDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case ItemAdded itemAdded:
                Apply(itemAdded);
                break;
            case ItemRemoved itemRemoved:
                Apply(itemRemoved);
                break;
            case DiscountApplied discountApplied:
                Apply(discountApplied);
                break;
            case CartCheckedOut cartCheckedOut:
                Apply(cartCheckedOut);
                break;
            default:
                throw new DomainException($"Unsupported event type: {domainEvent.GetType().Name}.");
        }
    }

    public void Apply(ItemAdded domainEvent)
    {
        var existingItem = _items.SingleOrDefault(item => item.ProductId == domainEvent.ProductId);
        if (existingItem is null)
        {
            _items.Add(new CartItem(
                domainEvent.ProductId,
                domainEvent.ProductName,
                domainEvent.Quantity,
                domainEvent.UnitPrice,
                domainEvent.SpecialInstructions));
        }
        else
        {
            existingItem.UpdateSnapshot(
                domainEvent.ProductName,
                domainEvent.UnitPrice,
                domainEvent.SpecialInstructions);
            existingItem.IncreaseQuantity(domainEvent.Quantity);
        }

        ResetDiscount();
        UpdatedAtUtc = domainEvent.OccurredAtUtc;
    }

    public void Apply(ItemRemoved domainEvent)
    {
        var item = _items.SingleOrDefault(i => i.ProductId == domainEvent.ProductId)
            ?? throw new DomainException("Cart item was not found.");

        _items.Remove(item);
        ResetDiscount();
        UpdatedAtUtc = domainEvent.OccurredAtUtc;
    }

    public void Apply(DiscountApplied domainEvent)
    {
        var normalizedPercentage = decimal.Round(domainEvent.Percentage, 2, MidpointRounding.AwayFromZero);
        var discountAmount = decimal.Round(SubtotalAmount * normalizedPercentage / 100m, 2, MidpointRounding.AwayFromZero);

        DiscountAmount = discountAmount;
        DiscountReason = string.IsNullOrWhiteSpace(domainEvent.Reason) ? null : domainEvent.Reason.Trim();
        UpdatedAtUtc = domainEvent.OccurredAtUtc;
    }

    public void Apply(CartCheckedOut domainEvent)
    {
        Status = CartStatus.CheckedOut;
        CheckedOutAtUtc = domainEvent.OccurredAtUtc;
        UpdatedAtUtc = domainEvent.OccurredAtUtc;
    }

    private void Raise(IShoppingCartDomainEvent domainEvent)
    {
        Apply(domainEvent);
        _uncommittedEvents.Add(domainEvent);
    }

    private CartItem FindItem(Guid productId)
    {
        var item = _items.SingleOrDefault(i => i.ProductId == productId);
        if (item is null)
        {
            throw new DomainException("Cart item was not found.");
        }

        return item;
    }

    private static void EnsureValidQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }
    }

    private void EnsureActive()
    {
        if (Status != CartStatus.Active)
        {
            throw new DomainException("Only active carts can be modified.");
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void ResetDiscount()
    {
        DiscountAmount = 0m;
        DiscountReason = null;
    }
}
