using Microsoft.EntityFrameworkCore;
using ShoppingCartService.Application.Abstractions;
using ShoppingCartService.Infrastructure.Persistence;
using ShoppingCartService.Infrastructure.Producer;
using ShoppingCartService.Infrastructure.Services;

namespace ShoppingCartService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddShoppingCartModule(this IServiceCollection services, IConfiguration configuration)
    {
        var shoppingCartConnectionString = configuration.GetConnectionString("ShoppingCartDb")
                                           ?? throw new InvalidOperationException("Connection string 'ShoppingCartDb' is not configured.");
        var eventStoreConnectionString = configuration.GetConnectionString("EventStoreDb")
                                        ?? throw new InvalidOperationException("Connection string 'EventStoreDb' is not configured.");

        services.AddDbContext<ShoppingCartDbContext>(options =>
            options.UseNpgsql(shoppingCartConnectionString,
                x => x.MigrationsAssembly(typeof(ShoppingCartDbContext).Assembly.FullName)));
        services.AddDbContext<EventStoreDbContext>(options =>
            options.UseNpgsql(eventStoreConnectionString,
                x => x.MigrationsAssembly(typeof(EventStoreDbContext).Assembly.FullName)));
        services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IShoppingCartEventStore, ShoppingCartEventStore>();
        services.AddScoped<IShoppingCartService, Application.Services.ShoppingCartService>();

        services.AddSingleton<IKafkaProducer, KafkaProducer>();
        services.AddHostedService<OutboxBackgroundService>();

        return services;
    }
}
