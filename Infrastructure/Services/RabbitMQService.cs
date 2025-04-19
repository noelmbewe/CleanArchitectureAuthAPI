using Application.Interfaces;
using RabbitMQ.Client;
using System.Text;

namespace Infrastructure.Services;

public class RabbitMQService : IMessageBroker
{
    private readonly ConnectionFactory _factory;

    public RabbitMQService()
    {
        _factory = new ConnectionFactory { HostName = "localhost" };
    }

    public async Task PublishAsync(string queueName, string message)
    {
        using var connection = _factory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

        var body = Encoding.UTF8.GetBytes(message);
        channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: null, body: body);
        await Task.CompletedTask;
    }
}