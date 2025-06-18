using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderService.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OrderService.Services
{
    public class MemberRegisteredConsumer : BackgroundService
    {
        private readonly ILogger<MemberRegisteredConsumer> _logger;
        private readonly RabbitMqSettings _rabbitMQSettings;

        public MemberRegisteredConsumer(
            IOptions<RabbitMqSettings> rabbitMQSettings,
            ILogger<MemberRegisteredConsumer> logger)
        {
            _rabbitMQSettings = rabbitMQSettings.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[RabbitMQ] MemberRegisteredConsumer başlatılıyor...");
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

            channel.QueueDeclare(queue: "member-registered",
                                durable: true,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);
            _logger.LogInformation("[RabbitMQ] Queue declare edildi.");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    _logger.LogInformation($"[RabbitMQ] MemberRegistered event alındı: {message}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing member-registered event: {ex.Message}");
                }
            };

            channel.BasicConsume(queue: "member-registered",
                                autoAck: true,
                                consumer: consumer);
            _logger.LogInformation("[RabbitMQ] Consumer başlatıldı.");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
} 