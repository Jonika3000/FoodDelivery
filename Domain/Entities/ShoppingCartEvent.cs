namespace ShoppingCartService.Domain.Entities;

public sealed class ShoppingCartEvent
{
    private ShoppingCartEvent()
    {
    }

    public ShoppingCartEvent(Guid cartId, Guid customerId, string eventType, string payload)
    {
        Id = Guid.NewGuid();
        CartId = cartId;
        CustomerId = customerId;
        EventType = eventType;
        Payload = payload;
        OccurredAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid CartId { get; private set; }

    public Guid CustomerId { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public DateTime OccurredAtUtc { get; private set; }
}
