using Confluent.Kafka;

namespace ShoppingCartService.Infrastructure.Producer;

public sealed class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(IConfiguration configuration, ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        
        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        
        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(string topic, string key, string value, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new Message<string, string> { Key = key, Value = value };
            var result = await _producer.ProduceAsync(topic, message, cancellationToken);
            
            _logger.LogInformation("Message delivered to {TopicPartitionOffset}", result.TopicPartitionOffset);
        }
        catch (ProduceException<string, string> e)
        {
            _logger.LogError(e, "Delivery failed: {Reason}", e.Error.Reason);
            throw;
        }
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}