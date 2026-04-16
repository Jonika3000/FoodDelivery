namespace ShoppingCartService.Domain.Entities;

public sealed class OutboxMessage
{
    private OutboxMessage() { }

    public OutboxMessage(Guid id, string eventType, string payload)
    {
        Id = id;
        EventType = eventType;
        Payload = payload;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? ProcessedAtUtc { get; private set; }

    public string? Error { get; private set; }

    public int RetryCount { get; private set; }

    public DateTime? LastAttemptAtUtc { get; private set; }

    public bool IsDeadLetter { get; private set; }

    public void MarkAsProcessed()
    {
        ProcessedAtUtc = DateTime.UtcNow;
        LastAttemptAtUtc = ProcessedAtUtc;
    }

    public void MarkAsFailed(string error)
    {
        Error = error;
        RetryCount++;
        LastAttemptAtUtc = DateTime.UtcNow;
    }

    public void MarkAsDeadLetter()
    {
        IsDeadLetter = true;
    }
}
