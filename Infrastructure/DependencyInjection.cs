using Microsoft.EntityFrameworkCore;
using ShoppingCartService.Application.Abstractions;
using ShoppingCartService.Application.Services;
using ShoppingCartService.Infrastructure.Persistence;

namespace ShoppingCartService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddShoppingCartModule(this IServiceCollection services, IConfiguration configuration)
    {
        var shoppingCartConnectionString = configuration.GetConnectionString("ShoppingCartDb")
                                           ?? throw new InvalidOperationException("Connection string 'ShoppingCartDb' is not configured.");
        var eventStoreConnectionString = configuration.GetConnectionString("EventStoreDb")
                                        ?? throw new InvalidOperationException("Connection string 'EventStoreDb' is not configured.");

        services.AddDbContext<ShoppingCartDbContext>(options => options.UseNpgsql(shoppingCartConnectionString));
        services.AddDbContext<EventStoreDbContext>(options => options.UseNpgsql(eventStoreConnectionString));
        services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();
        services.AddScoped<IShoppingCartEventStore, ShoppingCartEventStore>();
        services.AddScoped<IShoppingCartService, Application.Services.ShoppingCartService>();

        return services;
    }
}
