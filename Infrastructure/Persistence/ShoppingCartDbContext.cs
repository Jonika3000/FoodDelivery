using Microsoft.EntityFrameworkCore;
using ShoppingCartService.Domain.Entities;

namespace ShoppingCartService.Infrastructure.Persistence;

public sealed class ShoppingCartDbContext(DbContextOptions<ShoppingCartDbContext> options) : DbContext(options)
{
    public DbSet<ShoppingCart> ShoppingCarts => Set<ShoppingCart>();

    public DbSet<CartItem> CartItems => Set<CartItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        modelBuilder.Entity<ShoppingCart>(builder =>
        {
            builder.ToTable("shopping_cart_service");
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
            builder.ToTable("shopping_cart_service_items");
            builder.HasKey(item => item.Id);

            builder.Property(item => item.ProductId).IsRequired();
            builder.Property(item => item.ProductName).HasMaxLength(200).IsRequired();
            builder.Property(item => item.Quantity).IsRequired();
            builder.Property(item => item.UnitPrice).HasPrecision(18, 2).IsRequired();
            builder.Property(item => item.SpecialInstructions).HasMaxLength(500);

            builder.HasIndex(item => new { item.ShoppingCartId, item.ProductId }).IsUnique();
        });
    }
}
