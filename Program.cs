using ShoppingCartService.Infrastructure;
using ShoppingCartService.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();
builder.Services.AddShoppingCartModule(builder.Configuration);

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ShoppingCartService.Infrastructure.Persistence.ShoppingCartDbContext>();
    var eventStoreDbContext = scope.ServiceProvider.GetRequiredService<ShoppingCartService.Infrastructure.Persistence.EventStoreDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    await eventStoreDbContext.Database.EnsureCreatedAsync();
}

app.MapGrpcService<ShoppingCartGrpcService>();
app.MapGet("/", () => "Use a gRPC client to communicate with ShoppingCartGrpc.");

app.Run();
