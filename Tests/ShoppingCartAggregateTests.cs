using ShoppingCartService.Domain.Entities;
using ShoppingCartService.Domain.Enums;
using Xunit;

namespace ShoppingCartService.Tests;

public sealed class ShoppingCartAggregateTests
{
    [Fact]
    public void ShoppingCart_lifecycle_can_be_rebuilt_from_history()
    {
        var customerId = Guid.NewGuid();
        var restaurantId = Guid.NewGuid();
        var product1 = Guid.NewGuid();
        var product2 = Guid.NewGuid();
        var product3 = Guid.NewGuid();

        var cart = new ShoppingCart(customerId, restaurantId, "Pizza Place", "usd");

        cart.AddItem(product1, "Margherita", 1, 10m, null);
        cart.AddItem(product2, "Pepperoni", 2, 20m, null);
        cart.AddItem(product3, "Cola", 1, 15m, "No ice");
        cart.RemoveItem(product2);
        cart.ApplyDiscount(10m, "Promo");
        cart.Checkout();

        var restoredCart = ShoppingCart.FromHistory(
            customerId,
            restaurantId,
            "Pizza Place",
            "usd",
            cart.UncommittedEvents);

        Assert.Equal(6, cart.UncommittedEvents.Count);
        Assert.Equal(CartStatus.CheckedOut, restoredCart.Status);
        Assert.Equal(2, restoredCart.Items.Count);
        Assert.Equal(2, restoredCart.TotalItems);
        Assert.Equal(25m, restoredCart.SubtotalAmount);
        Assert.Equal(2.50m, restoredCart.DiscountAmount);
        Assert.Equal(22.50m, restoredCart.TotalAmount);
        Assert.Equal("Promo", restoredCart.DiscountReason);
        Assert.NotNull(restoredCart.CheckedOutAtUtc);
        Assert.DoesNotContain(restoredCart.Items, item => item.ProductId == product2);
    }
}
