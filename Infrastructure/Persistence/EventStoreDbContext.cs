using Microsoft.EntityFrameworkCore;
using ShoppingCartService.Domain.Entities;

namespace ShoppingCartService.Infrastructure.Persistence;

public sealed class EventStoreDbContext(DbContextOptions<EventStoreDbContext> options) : DbContext(options)
{
    public DbSet<ShoppingCartEvent> ShoppingCartEvents => Set<ShoppingCartEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        modelBuilder.Entity<ShoppingCartEvent>(builder =>
        {
            builder.ToTable("shopping_cart_events");
            builder.HasKey(@event => @event.Id);

            builder.Property(@event => @event.CartId).IsRequired();
            builder.Property(@event => @event.CustomerId).IsRequired();
            builder.Property(@event => @event.EventType).HasMaxLength(128).IsRequired();
            builder.Property(@event => @event.Payload).HasColumnType("jsonb").IsRequired();
            builder.Property(@event => @event.OccurredAtUtc).IsRequired();

            builder.HasIndex(@event => new { @event.CartId, @event.OccurredAtUtc });
            builder.HasIndex(@event => new { @event.CustomerId, @event.OccurredAtUtc });
        });
    }
}
