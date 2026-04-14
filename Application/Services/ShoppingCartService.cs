using ShoppingCartService.Application.Abstractions;
using ShoppingCartService.Application.Contracts;
using ShoppingCartService.Application.Mapping;
using ShoppingCartService.Domain.Entities;
using ShoppingCartService.Domain.Exceptions;
using System.Text.Json;

namespace ShoppingCartService.Application.Services;

public sealed class ShoppingCartService(
    IShoppingCartRepository shoppingCartRepository,
    IShoppingCartEventStore shoppingCartEventStore) : IShoppingCartService
{
    public async Task<CartDto?> GetActiveCartAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var cart = await shoppingCartRepository.GetActiveByCustomerIdAsync(customerId, cancellationToken);
        return cart?.ToDto();
    }

    public async Task<CartDto> AddItemAsync(Guid customerId, AddCartItemRequest request, CancellationToken cancellationToken)
    {
        ValidateCustomer(customerId);

        if (await IsAlreadyProcessedAsync(request.RequestId, cancellationToken))
        {
            var cartDto = await GetActiveCartAsync(customerId, cancellationToken);
            return cartDto ?? throw new DomainException("Active cart was not found.");
        }

        var cart = await shoppingCartRepository.GetActiveByCustomerIdAsync(customerId, cancellationToken);
        if (cart is null)
        {
            cart = new ShoppingCart(customerId, request.RestaurantId, request.RestaurantName, request.Currency);
            await shoppingCartRepository.AddAsync(cart, cancellationToken);
        }
        else if (cart.RestaurantId != request.RestaurantId)
        {
            throw new DomainException("Customer already has an active cart for another restaurant.");
        }

        cart.AddItem(
            request.ProductId,
            request.ProductName,
            request.Quantity,
            request.UnitPrice,
            request.SpecialInstructions);

        await SaveCartAndEventAsync(
            cart,
            "ItemAdded",
            new
            {
                request.ProductId,
                request.ProductName,
                request.Quantity,
                request.UnitPrice,
                request.SpecialInstructions
            },
            request.RequestId,
            cancellationToken);

        return cart.ToDto();
    }

    public async Task<CartDto> UpdateItemQuantityAsync(Guid customerId, Guid productId, UpdateCartItemQuantityRequest request, CancellationToken cancellationToken)
    {
        ValidateCustomer(customerId);

        if (await IsAlreadyProcessedAsync(request.RequestId, cancellationToken))
        {
            var cartDto = await GetActiveCartAsync(customerId, cancellationToken);
            return cartDto ?? throw new DomainException("Active cart was not found.");
        }

        var cart = await GetRequiredCartAsync(customerId, cancellationToken);
        cart.ChangeItemQuantity(productId, request.Quantity);

        await SaveCartAndEventAsync(
            cart,
            "ItemQuantityUpdated",
            new { ProductId = productId, Quantity = request.Quantity },
            request.RequestId,
            cancellationToken);

        return cart.ToDto();
    }

    public async Task DeleteItemAsync(Guid customerId, Guid productId, string? requestId, CancellationToken cancellationToken)
    {
        ValidateCustomer(customerId);

        if (await IsAlreadyProcessedAsync(requestId, cancellationToken)) return;

        var cart = await GetRequiredCartAsync(customerId, cancellationToken);
        cart.RemoveItem(productId);

        await SaveCartAndEventAsync(
            cart,
            "ItemRemoved",
            new { ProductId = productId },
            requestId,
            cancellationToken);
    }

    public async Task ClearAsync(Guid customerId, string? requestId, CancellationToken cancellationToken)
    {
        ValidateCustomer(customerId);

        if (await IsAlreadyProcessedAsync(requestId, cancellationToken)) return;

        var cart = await GetRequiredCartAsync(customerId, cancellationToken);
        cart.Clear();

        await SaveCartAndEventAsync(cart, "CartCleared", new { }, requestId, cancellationToken);
    }

    public async Task<CartDto> DiscountAppliedAsync(Guid customerId, DiscountAppliedRequest request, CancellationToken cancellationToken)
    {
        ValidateCustomer(customerId);

        if (await IsAlreadyProcessedAsync(request.RequestId, cancellationToken))
        {
            var cartDto = await GetActiveCartAsync(customerId, cancellationToken);
            return cartDto ?? throw new DomainException("Active cart was not found.");
        }

        var cart = await GetRequiredCartAsync(customerId, cancellationToken);
        cart.DiscountApplied(request.Amount, request.Reason);

        await SaveCartAndEventAsync(
            cart,
            "DiscountApplied",
            new
            {
                request.Amount,
                request.Reason,
                cart.TotalAmount
            },
            request.RequestId,
            cancellationToken);

        return cart.ToDto();
    }

    public async Task<CartDto> CheckoutAsync(Guid customerId, string? requestId, CancellationToken cancellationToken)
    {
        ValidateCustomer(customerId);

        if (await IsAlreadyProcessedAsync(requestId, cancellationToken))
        {
            var cartDto = await GetActiveCartAsync(customerId, cancellationToken);
            return cartDto ?? throw new DomainException("Active cart was not found.");
        }

        var cart = await GetRequiredCartAsync(customerId, cancellationToken);
        cart.Checkout();

        await SaveCartAndEventAsync(cart, "CartCheckedOut", new { cart.CheckedOutAtUtc }, requestId, cancellationToken);
        return cart.ToDto();
    }

    private async Task<ShoppingCart> GetRequiredCartAsync(Guid customerId, CancellationToken cancellationToken)
    {
        return await shoppingCartRepository.GetActiveByCustomerIdAsync(customerId, cancellationToken)
            ?? throw new DomainException("Active cart was not found.");
    }

    private static void ValidateCustomer(Guid customerId)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException("CustomerId is required.");
        }
    }

    private async Task<bool> IsAlreadyProcessedAsync(string? requestId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestId)) return false;
        return await shoppingCartEventStore.HasBeenProcessedAsync(requestId, cancellationToken);
    }

    private async Task SaveCartAndEventAsync(
        ShoppingCart cart,
        string eventType,
        object payload,
        string? requestId,
        CancellationToken cancellationToken)
    {
        await shoppingCartRepository.SaveChangesAsync(cancellationToken);

        var shoppingCartEvent = new ShoppingCartEvent(
            cart.Id,
            cart.CustomerId,
            eventType,
            JsonSerializer.Serialize(payload));

        await shoppingCartEventStore.AppendAsync(shoppingCartEvent, cancellationToken);
        
        if (!string.IsNullOrWhiteSpace(requestId))
        {
            await shoppingCartEventStore.MarkAsProcessedAsync(requestId, cancellationToken);
        }
        
        await shoppingCartEventStore.SaveChangesAsync(cancellationToken);
    }
}
