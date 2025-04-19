namespace Application.Interfaces;

public interface IMessageBroker
{
    Task PublishAsync(string queueName, string message);
}