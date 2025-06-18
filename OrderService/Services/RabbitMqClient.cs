using Microsoft.Extensions.Options;
using OrderService.Configuration;
using OrderService.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client.Events;

namespace OrderService.Services
{
    public class RabbitMqClient : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private const string QueueName = "order_created";
        private const int MaxRetries = 5;
        private const int RetryDelayMs = 5000;

        public RabbitMqClient(IOptions<RabbitMqSettings> settings)
        {
            var factory = new ConnectionFactory
            {
                HostName = settings.Value.HostName,
                UserName = settings.Value.UserName,
                Password = settings.Value.Password,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(30),
                SocketReadTimeout = TimeSpan.FromSeconds(30),
                SocketWriteTimeout = TimeSpan.FromSeconds(30)
            };

            int retryCount = 0;
            while (retryCount < MaxRetries)
            {
                try
                {
                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();

                    // Declare queue as durable
                    _channel.QueueDeclare(
                        queue: QueueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);

                    Console.WriteLine("Successfully connected to RabbitMQ");
                    break;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    Console.WriteLine($"Failed to connect to RabbitMQ (attempt {retryCount}/{MaxRetries}): {ex.Message}");
                    
                    if (retryCount == MaxRetries)
                    {
                        throw;
                    }
                    
                    Thread.Sleep(RetryDelayMs);
                }
            }
        }

        public void PublishOrderCreated(OrderDto order)
        {
            var message = JsonSerializer.Serialize(order);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true; // Make message persistent

            _channel.BasicPublish(
                exchange: "",
                routingKey: QueueName,
                basicProperties: properties,
                body: body);
        }

        public void ConsumeInvoiceRequests()
        {
            var consumer = new EventingBasicConsumer(_channel);
            
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                var invoiceRequest = JsonSerializer.Deserialize<InvoiceRequestDto>(message);
                
                Console.WriteLine($"Received invoice request for Order ID: {invoiceRequest.OrderId}");
                Console.WriteLine($"Customer: {invoiceRequest.CustomerName}");
                Console.WriteLine($"Amount: {invoiceRequest.Amount}");
                Console.WriteLine($"Request Date: {invoiceRequest.RequestDate}");
            };

            _channel.BasicConsume(
                queue: QueueName,
                autoAck: true,
                consumer: consumer);
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
} 