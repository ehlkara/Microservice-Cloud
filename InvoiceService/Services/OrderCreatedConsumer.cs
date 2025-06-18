using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using InvoiceService.Configuration;
using InvoiceService.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Utils;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace InvoiceService.Services
{
    public class OrderCreatedConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrderCreatedConsumer> _logger;
        private readonly RabbitMqSettings _rabbitMQSettings;
        private readonly MongoDbSettings _mongoDbSettings;

        public OrderCreatedConsumer(
            IServiceProvider serviceProvider,
            IOptions<RabbitMqSettings> rabbitMQSettings,
            IOptions<MongoDbSettings> mongoDbSettings,
            ILogger<OrderCreatedConsumer> logger)
        {
            _serviceProvider = serviceProvider;
            _rabbitMQSettings = rabbitMQSettings.Value;
            _mongoDbSettings = mongoDbSettings.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[RabbitMQ] Background consumer başlatılıyor...");
            var factory = new ConnectionFactory
            {
                HostName = _rabbitMQSettings.HostName,
                UserName = _rabbitMQSettings.UserName,
                Password = _rabbitMQSettings.Password
            };

            using var connection = factory.CreateConnection();
            _logger.LogInformation("[RabbitMQ] Bağlantı açıldı.");
            using var channel = connection.CreateModel();
            _logger.LogInformation("[RabbitMQ] Channel açıldı.");

            channel.QueueDeclare(queue: "order-created",
                                durable: true,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);
            _logger.LogInformation("[RabbitMQ] Queue declare edildi.");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var orderCreatedEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(message);

                    var client = new MongoClient(_mongoDbSettings.ConnectionString);
                    var database = client.GetDatabase(_mongoDbSettings.DatabaseName);
                    var invoices = database.GetCollection<Invoice>("Invoices");

                    var invoice = new Invoice
                    {
                        OrderId = orderCreatedEvent.OrderId,
                        Amount = orderCreatedEvent.Price * orderCreatedEvent.Quantity,
                        CreatedAt = DateTime.UtcNow
                    };

                    await invoices.InsertOneAsync(invoice);
                    _logger.LogInformation($"Invoice created for order {orderCreatedEvent.OrderId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing order-created event: {ex.Message}");
                }
            };

            channel.BasicConsume(queue: "order-created",
                                autoAck: true,
                                consumer: consumer);
            _logger.LogInformation("[RabbitMQ] Consumer başlatıldı.");

            // Keep the background service alive
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
} 