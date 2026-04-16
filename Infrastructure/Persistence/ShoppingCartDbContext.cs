using Microsoft.EntityFrameworkCore;
using ShoppingCartService.Domain.Abstractions;
using ShoppingCartService.Domain.Entities;

namespace ShoppingCartService.Infrastructure.Persistence;

public sealed class ShoppingCartDbContext(DbContextOptions<ShoppingCartDbContext> options) : DbContext(options)
{
    public DbSet<ShoppingCart> ShoppingCarts => Set<ShoppingCart>();

    public DbSet<CartItem> CartItems => Set<CartItem>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        modelBuilder.Entity<ShoppingCart>(builder =>
        {
            builder.ToTable("shopping_cart");
            builder.HasKey(cart => cart.Id);

            builder.Property(cart => cart.CustomerId).IsRequired();
            builder.Property(cart => cart.RestaurantId).IsRequired();
            builder.Property(cart => cart.RestaurantName).HasMaxLength(200).IsRequired();
            builder.Property(cart => cart.Currency).HasMaxLength(8).IsRequired();
            builder.Property(cart => cart.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            builder.Property(cart => cart.DiscountAmount).HasPrecision(18, 2).IsRequired();
            builder.Property(cart => cart.DiscountReason).HasMaxLength(250);
            builder.Property(cart => cart.CreatedAtUtc).IsRequired();
            builder.Property(cart => cart.UpdatedAtUtc).IsRequired();

            builder.HasIndex(cart => new { cart.CustomerId, cart.Status });

            builder.Metadata.FindNavigation(nameof(ShoppingCart.Items))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(cart => cart.Items)
                .HasField("_items");

            builder.HasMany(cart => cart.Items)
                .WithOne()
                .HasForeignKey(nameof(CartItem.ShoppingCartId))
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItem>(builder =>
        {
            builder.ToTable("shopping_cart_items");
            builder.HasKey(item => item.Id);

            builder.Property(item => item.ProductId).IsRequired();
            builder.Property(item => item.ProductName).HasMaxLength(200).IsRequired();
            builder.Property(item => item.Quantity).IsRequired();
            builder.Property(item => item.UnitPrice).HasPrecision(18, 2).IsRequired();
            builder.Property(item => item.SpecialInstructions).HasMaxLength(500);

            builder.HasIndex(item => new { item.ShoppingCartId, item.ProductId }).IsUnique();
        });

        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("outbox_messages");
            builder.HasKey(m => m.Id);

            builder.Property(m => m.EventType).IsRequired();
            builder.Property(m => m.Payload).IsRequired();
            builder.Property(m => m.CreatedAtUtc).IsRequired();
            builder.Property(m => m.ProcessedAtUtc);
            builder.Property(m => m.Error);
            builder.Property(m => m.RetryCount).IsRequired();
            builder.Property(m => m.LastAttemptAtUtc);
            builder.Property(m => m.IsDeadLetter).IsRequired();

            builder.HasIndex(m => m.ProcessedAtUtc).HasFilter("\"ProcessedAtUtc\" IS NULL AND \"RetryCount\" < 3 AND \"IsDeadLetter\" = FALSE");
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEvents = ChangeTracker.Entries<IAggregateRoot>()
            .SelectMany(x =>
            {
                var events = x.Entity.DomainEvents.ToList();
                x.Entity.ClearDomainEvents();
                return events;
            })
            .ToList();

        var outboxMessages = domainEvents.Select(domainEvent => new OutboxMessage(
            Guid.NewGuid(),
            domainEvent.GetType().Name,
            System.Text.Json.JsonSerializer.Serialize(domainEvent, domainEvent.GetType())
        )).ToList();

        await OutboxMessages.AddRangeAsync(outboxMessages, cancellationToken);

        return await base.SaveChangesAsync(cancellationToken);
    }
}
