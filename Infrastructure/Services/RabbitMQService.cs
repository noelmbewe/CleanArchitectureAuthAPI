using RabbitMQ.Client;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Application.Interfaces;

namespace Infrastructure.Services;

public class RabbitMQService : IMessageBroker, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _dlxExchange = "email.dlx";
    private readonly string _retryQueueName = "email_queue.retry";

    public RabbitMQService()
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(_dlxExchange, ExchangeType.Direct, durable: true);
    }

    public async Task PublishAsync(string queue, string message)
    {
        try
        {
            var queueArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", _dlxExchange },
                { "x-dead-letter-routing-key", _retryQueueName }
            };
            _channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
            _channel.QueueBind(queue, _dlxExchange, queue);

            var body = Encoding.UTF8.GetBytes(message);
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            await Task.Run(() =>
            {
                _channel.BasicPublish(exchange: "", routingKey: queue, basicProperties: properties, body: body);
            });
            Log.Information("Published message to queue {Queue}: {Message}", queue, message);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to publish message to queue {Queue}: {Message}", queue, message);
            throw;
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}