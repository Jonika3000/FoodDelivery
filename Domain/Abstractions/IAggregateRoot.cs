namespace ShoppingCartService.Domain.Abstractions;

public interface IDomainEvent
{
    DateTime OccurredAtUtc { get; }
}

public interface IAggregateRoot
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
