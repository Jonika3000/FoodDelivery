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
            cancellationToken);

        return cart.ToDto();
    }

    public async Task<CartDto> UpdateItemQuantityAsync(Guid customerId, Guid productId, int quantity, CancellationToken cancellationToken)
    {
        ValidateCustomer(customerId);

        var cart = await GetRequiredCartAsync(customerId, cancellationToken);
        cart.ChangeItemQuantity(productId, quantity);

        await SaveCartAndEventAsync(
            cart,
            "ItemQuantityUpdated",
            new { ProductId = productId, Quantity = quantity },
            cancellationToken);

        return cart.ToDto();
    }

    public async Task DeleteItemAsync(Guid customerId, Guid productId, CancellationToken cancellationToken)
    {
        ValidateCustomer(customerId);

        var cart = await GetRequiredCartAsync(customerId, cancellationToken);
        cart.RemoveItem(productId);

        await SaveCartAndEventAsync(
            cart,
            "ItemRemoved",
            new { ProductId = productId },
            cancellationToken);
    }

    public async Task ClearAsync(Guid customerId, CancellationToken cancellationToken)
    {
        ValidateCustomer(customerId);

        var cart = await GetRequiredCartAsync(customerId, cancellationToken);
        cart.Clear();

        await SaveCartAndEventAsync(cart, "CartCleared", new { }, cancellationToken);
    }

    public async Task<CartDto> DiscountAppliedAsync(Guid customerId, DiscountAppliedRequest request, CancellationToken cancellationToken)
    {
        ValidateCustomer(customerId);

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
            cancellationToken);

        return cart.ToDto();
    }

    public async Task<CartDto> CheckoutAsync(Guid customerId, CancellationToken cancellationToken)
    {
        ValidateCustomer(customerId);

        var cart = await GetRequiredCartAsync(customerId, cancellationToken);
        cart.Checkout();

        await SaveCartAndEventAsync(cart, "CartCheckedOut", new { cart.CheckedOutAtUtc }, cancellationToken);
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

    private async Task SaveCartAndEventAsync(
        ShoppingCart cart,
        string eventType,
        object payload,
        CancellationToken cancellationToken)
    {
        await shoppingCartRepository.SaveChangesAsync(cancellationToken);

        var shoppingCartEvent = new ShoppingCartEvent(
            cart.Id,
            cart.CustomerId,
            eventType,
            JsonSerializer.Serialize(payload));

        await shoppingCartEventStore.AppendAsync(shoppingCartEvent, cancellationToken);
        await shoppingCartEventStore.SaveChangesAsync(cancellationToken);
    }
}
