using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ShoppingCartService.Application.Abstractions;
using ShoppingCartService.Domain.Entities;
using ShoppingCartService.Infrastructure.Persistence;
using ShoppingCartService.Infrastructure.Producer;

namespace ShoppingCartService.Infrastructure.Services;

public class OutboxBackgroundService(
    IServiceProvider serviceProvider,
    IKafkaProducer kafkaProducer,
    ILogger<OutboxBackgroundService> logger) : BackgroundService
{
    private const string OutboxTopic = "shopping-cart-outbox";
    private const string DeadLetterTopic = "shopping-cart-outbox-dlq";
    private const int MaxRetryCount = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox Background Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while processing outbox messages.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        logger.LogInformation("Outbox Background Service is stopping.");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShoppingCartDbContext>();

        var messages = await outboxRepository.GetUnprocessedMessagesAsync(20, stoppingToken);

        if (messages.Count == 0)
        {
            return;
        }

        logger.LogInformation("Processing {Count} outbox messages.", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                var eventType = GetEventType(message.EventType);
                if (eventType == null)
                {
                    logger.LogWarning("Unknown event type: {EventType}", message.EventType);
                    message.MarkAsProcessed();
                    continue;
                }

                var domainEvent = (IShoppingCartDomainEvent?)JsonSerializer.Deserialize(message.Payload, eventType);
                if (domainEvent == null)
                {
                    logger.LogWarning("Failed to deserialize event: {MessageId}", message.Id);
                    message.MarkAsProcessed();
                    continue;
                }

                var cart = await dbContext.ShoppingCarts
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.Id == domainEvent.CartId, stoppingToken);

                logger.LogInformation("Publishing event {EventType} with payload {Payload} to Kafka", message.EventType, message.Payload);

                await kafkaProducer.PublishAsync(
                    OutboxTopic,
                    message.Id.ToString(),
                    message.Payload,
                    stoppingToken);

                message.MarkAsProcessed();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
                message.MarkAsFailed(ex.Message);

                if (message.RetryCount >= MaxRetryCount)
                {
                    logger.LogWarning("Message {MessageId} reached max retry count. Moving to Dead Letter Queue.", message.Id);
                    message.MarkAsDeadLetter();
                    
                    try
                    {
                        await kafkaProducer.PublishAsync(
                            DeadLetterTopic,
                            message.Id.ToString(),
                            message.Payload,
                            stoppingToken);
                    }
                    catch (Exception dlqEx)
                    {
                        logger.LogError(dlqEx, "Failed to publish message {MessageId} to Dead Letter Topic", message.Id);
                    }
                }
            }
        }

        await outboxRepository.SaveChangesAsync(stoppingToken);
    }

    private static Type? GetEventType(string typeName)
    {
        return typeName switch
        {
            nameof(ItemAdded) => typeof(ItemAdded),
            nameof(ItemRemoved) => typeof(ItemRemoved),
            nameof(DiscountApplied) => typeof(DiscountApplied),
            nameof(CartCheckedOut) => typeof(CartCheckedOut),
            nameof(CartCreated) => typeof(CartCreated),
            _ => null
        };
    }
}
