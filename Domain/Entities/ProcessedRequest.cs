namespace ShoppingCartService.Domain.Entities;

public sealed class ProcessedRequest(string requestId)
{
    public string RequestId { get; private set; } = requestId;
    public DateTime ProcessedAtUtc { get; private set; } = DateTime.UtcNow;
}