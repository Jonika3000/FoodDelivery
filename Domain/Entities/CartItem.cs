using ShoppingCartService.Domain.Exceptions;

namespace ShoppingCartService.Domain.Entities;

public class CartItem
{
    private CartItem()
    {
    }

    public CartItem(Guid productId, string productName, int quantity, decimal unitPrice, string? specialInstructions)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("ProductId is required.");
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new DomainException("Product name is required.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        if (unitPrice < 0)
        {
            throw new DomainException("Unit price cannot be negative.");
        }

        Id = Guid.NewGuid();
        ProductId = productId;
        ProductName = productName.Trim();
        Quantity = quantity;
        UnitPrice = decimal.Round(unitPrice, 2, MidpointRounding.AwayFromZero);
        SpecialInstructions = NormalizeInstructions(specialInstructions);
    }

    public Guid Id { get; private set; }

    public Guid ShoppingCartId { get; private set; }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public string? SpecialInstructions { get; private set; }

    public decimal TotalPrice => decimal.Round(UnitPrice * Quantity, 2, MidpointRounding.AwayFromZero);

    public void IncreaseQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity increment must be greater than zero.");
        }

        Quantity += quantity;
    }

    public void SetQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        Quantity = quantity;
    }

    public void UpdateSnapshot(string productName, decimal unitPrice, string? specialInstructions)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new DomainException("Product name is required.");
        }

        if (unitPrice < 0)
        {
            throw new DomainException("Unit price cannot be negative.");
        }

        ProductName = productName.Trim();
        UnitPrice = decimal.Round(unitPrice, 2, MidpointRounding.AwayFromZero);
        SpecialInstructions = NormalizeInstructions(specialInstructions);
    }

    private static string? NormalizeInstructions(string? specialInstructions)
    {
        return string.IsNullOrWhiteSpace(specialInstructions) ? null : specialInstructions.Trim();
    }
}
