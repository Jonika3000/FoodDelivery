using Grpc.Core;
using ShoppingCartService.Application.Abstractions;
using ShoppingCartService.Domain.Exceptions;
using ShoppingCartService.Protos;
using AddCartItemContract = ShoppingCartService.Application.Contracts.AddCartItemRequest;
using DiscountAppliedContract = ShoppingCartService.Application.Contracts.DiscountAppliedRequest;

namespace ShoppingCartService.Services;

public sealed class ShoppingCartGrpcService(IShoppingCartService shoppingCartService)
    : ShoppingCartGrpc.ShoppingCartGrpcBase
{
    public override async Task<CartReply> GetActiveCart(GetActiveCartRequest request, ServerCallContext context)
    {
        try
        {
            var customerId = ParseGuid(request.CustomerId, nameof(request.CustomerId));
            var cart = await shoppingCartService.GetActiveCartAsync(customerId, context.CancellationToken);

            if (cart is null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Active cart was not found."));
            }

            return cart.ToReply();
        }
        catch (DomainException exception)
        {
            throw ToRpcException(exception);
        }
    }

    public override async Task<CartReply> AddItem(AddItemRequest request, ServerCallContext context)
    {
        try
        {
            var cart = await shoppingCartService.AddItemAsync(
                ParseGuid(request.CustomerId, nameof(request.CustomerId)),
                new AddCartItemContract(
                    ParseGuid(request.RestaurantId, nameof(request.RestaurantId)),
                    request.RestaurantName,
                    request.Currency,
                    ParseGuid(request.ProductId, nameof(request.ProductId)),
                    request.ProductName,
                    request.Quantity,
                    Convert.ToDecimal(request.UnitPrice),
                    NormalizeOptional(request.SpecialInstructions)),
                context.CancellationToken);

            return cart.ToReply();
        }
        catch (DomainException exception)
        {
            throw ToRpcException(exception);
        }
    }

    public override async Task<CartReply> UpdateItemQuantity(UpdateItemQuantityRequest request, ServerCallContext context)
    {
        try
        {
            var cart = await shoppingCartService.UpdateItemQuantityAsync(
                ParseGuid(request.CustomerId, nameof(request.CustomerId)),
                ParseGuid(request.ProductId, nameof(request.ProductId)),
                request.Quantity,
                context.CancellationToken);

            return cart.ToReply();
        }
        catch (DomainException exception)
        {
            throw ToRpcException(exception);
        }
    }

    public override async Task<OperationReply> RemoveItem(RemoveItemRequest request, ServerCallContext context)
    {
        try
        {
            await shoppingCartService.DeleteItemAsync(
                ParseGuid(request.CustomerId, nameof(request.CustomerId)),
                ParseGuid(request.ProductId, nameof(request.ProductId)),
                context.CancellationToken);

            return new OperationReply { Success = true };
        }
        catch (DomainException exception)
        {
            throw ToRpcException(exception);
        }
    }

    public override async Task<OperationReply> ClearCart(ClearCartRequest request, ServerCallContext context)
    {
        try
        {
            await shoppingCartService.ClearAsync(
                ParseGuid(request.CustomerId, nameof(request.CustomerId)),
                context.CancellationToken);

            return new OperationReply { Success = true };
        }
        catch (DomainException exception)
        {
            throw ToRpcException(exception);
        }
    }

    public override async Task<CartReply> DiscountApplied(DiscountAppliedRequest request, ServerCallContext context)
    {
        try
        {
            var cart = await shoppingCartService.DiscountAppliedAsync(
                ParseGuid(request.CustomerId, nameof(request.CustomerId)),
                new DiscountAppliedContract(
                    Convert.ToDecimal(request.Amount),
                    NormalizeOptional(request.Reason)),
                context.CancellationToken);

            return cart.ToReply();
        }
        catch (DomainException exception)
        {
            throw ToRpcException(exception);
        }
    }

    public override async Task<CartReply> Checkout(CheckoutRequest request, ServerCallContext context)
    {
        try
        {
            var cart = await shoppingCartService.CheckoutAsync(
                ParseGuid(request.CustomerId, nameof(request.CustomerId)),
                context.CancellationToken);

            return cart.ToReply();
        }
        catch (DomainException exception)
        {
            throw ToRpcException(exception);
        }
    }

    private static Guid ParseGuid(string value, string fieldName)
    {
        return Guid.TryParse(value, out var parsed)
            ? parsed
            : throw new RpcException(new Status(StatusCode.InvalidArgument, $"{fieldName} must be a valid GUID."));
    }

    private static string? NormalizeOptional(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static RpcException ToRpcException(DomainException exception)
    {
        return new RpcException(new Status(StatusCode.InvalidArgument, exception.Message));
    }
}
