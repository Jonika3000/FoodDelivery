using Moq;
using ShoppingCartService.Application.Abstractions;
using ShoppingCartService.Application.Contracts;
using ShoppingCartService.Domain.Entities;
using Xunit;

namespace ShoppingCartService.Tests;

public sealed class ShoppingCartServiceIdempotencyTests
{
    private readonly Mock<IShoppingCartRepository> _repositoryMock = new();
    private readonly Mock<IShoppingCartEventStore> _eventStoreMock = new();
    private readonly Application.Services.ShoppingCartService _service;

    public ShoppingCartServiceIdempotencyTests()
    {
        _service = new Application.Services.ShoppingCartService(
            _repositoryMock.Object, 
            _eventStoreMock.Object);
    }

    [Fact]
    public async Task AddItem_should_be_idempotent()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var requestId = "test-request-id";
        var request = new AddCartItemRequest(
            Guid.NewGuid(), "Restaurant", "USD", 
            Guid.NewGuid(), "Product", 1, 10m, null, requestId);

        var cart = new ShoppingCart(customerId, request.RestaurantId, request.RestaurantName, request.Currency);

        _eventStoreMock.Setup(x => x.HasBeenProcessedAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock.Setup(x => x.GetActiveByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        // Act
        // Send message 3 times
        await _service.AddItemAsync(customerId, request, CancellationToken.None);
        
        // Simulate that after first call, the request is marked as processed
        _eventStoreMock.Setup(x => x.HasBeenProcessedAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
            
        await _service.AddItemAsync(customerId, request, CancellationToken.None);
        await _service.AddItemAsync(customerId, request, CancellationToken.None);

        // Assert
        // Verify that the business operation (adding item to cart) only happened once in the first call
        // In our case, the cart object will have only 1 item if the subsequent calls were skipped
        Assert.Single(cart.Items);
        
        // Verify that SaveChangesAsync and MarkAsProcessedAsync were called
        _repositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        
        _eventStoreMock.Verify(x => x.MarkAsProcessedAsync(requestId, It.IsAny<CancellationToken>()), Times.Once);
        _eventStoreMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}