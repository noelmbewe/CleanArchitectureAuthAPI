using Infrastructure.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.Distributed;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Infrastructure.Services;

public class EmailConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _queueName = "email_queue";
    private readonly string _retryQueueName = "email_queue.retry";
    private readonly string _dlqName = "email_queue.dlq";
    private readonly string _dlxExchange = "email.dlx";

    public EmailConsumerService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        var factory = new ConnectionFactory { HostName = "localhost", DispatchConsumersAsync = true };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(_dlxExchange, ExchangeType.Direct, durable: true);

        var queueArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", _dlxExchange },
            { "x-dead-letter-routing-key", _retryQueueName }
        };
        _channel.QueueDeclare(_queueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
        _channel.QueueBind(_queueName, _dlxExchange, _queueName);

        var retryArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", _dlxExchange },
            { "x-dead-letter-routing-key", _queueName },
            { "x-message-ttl", 60000 }
        };
        _channel.QueueDeclare(_retryQueueName, durable: true, exclusive: false, autoDelete: false, arguments: retryArgs);
        _channel.QueueBind(_retryQueueName, _dlxExchange, _retryQueueName);

        _channel.QueueDeclare(_dlqName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        _channel.QueueBind(_dlqName, _dlxExchange, _dlqName);

        Log.Information("EmailConsumerService initialized");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var retryCount = ea.BasicProperties.Headers != null && ea.BasicProperties.Headers.ContainsKey("x-retry-count")
                ? Convert.ToInt32(ea.BasicProperties.Headers["x-retry-count"])
                : 0;

            using var scope = _scopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();
            var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();

            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var parts = message.Split(':');
                var email = parts[0];
                var otp = parts[1];

                // Check OTP expiration
                var cachedOtp = await cache.GetStringAsync($"otp:{email}");
                if (string.IsNullOrEmpty(cachedOtp))
                {
                    Log.Information("OTP expired for {Email}, discarding message", email);
                    _channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                await emailService.SendEmailAsync(email, "Your OTP Code", $"Your OTP is {otp}. It expires in 30 minutes.");
                _channel.BasicAck(ea.DeliveryTag, false);
                Log.Information("Processed message for {Email}, retry count {RetryCount}", email, retryCount);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to process message, retry count {RetryCount}", retryCount);
                if (retryCount < 10)
                {
                    var properties = ea.BasicProperties;
                    properties.Headers ??= new Dictionary<string, object>();
                    properties.Headers["x-retry-count"] = retryCount + 1;

                    _channel.BasicPublish(_dlxExchange, _retryQueueName, properties, ea.Body);
                    Log.Information("Moved message to retry queue, retry count {RetryCount}", retryCount + 1);
                }
                else
                {
                    _channel.BasicPublish(_dlxExchange, _dlqName, ea.BasicProperties, ea.Body);
                    Log.Error("Max retries reached, moved to DLQ");
                }
                _channel.BasicNack(ea.DeliveryTag, false, false);
            }
        };

        _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
        Log.Information("Started consuming messages from {QueueName}", _queueName);
        await Task.CompletedTask;

        stoppingToken.Register(() =>
        {
            _channel.Close();
            _connection.Close();
            Log.Information("EmailConsumerService stopped");
        });
    }
}