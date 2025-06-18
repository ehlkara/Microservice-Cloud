using Microsoft.AspNetCore.Mvc;
using MembershipService.Models;
using MembershipService.Events;
using MongoDB.Driver;
using MembershipService.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace MembershipService.Controllers
{
    [ApiController]
    [Route("snapshot")]
    public class MarkerController : ControllerBase
    {
        private static bool _markerReceived = false;
        private static readonly object _lock = new();
        private readonly IMongoDatabase _mongoDatabase;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly List<string> _otherNodes = new() { "http://localhost:5002", "http://localhost:5003" }; // Kendi adresinizi hariç tutun
        private readonly RabbitMqSettings _rabbitMQSettings;

        public MarkerController(IMongoDatabase mongoDatabase, IHttpClientFactory httpClientFactory, IOptions<RabbitMqSettings> rabbitMQSettings)
        {
            _mongoDatabase = mongoDatabase;
            _httpClientFactory = httpClientFactory;
            _rabbitMQSettings = rabbitMQSettings.Value;
        }

        [HttpPost("marker")]
        public async Task<IActionResult> ReceiveMarker()
        {
            lock (_lock)
            {
                if (!_markerReceived)
                {
                    _markerReceived = true;
                    // LocalState'i kaydet
                    var localState = new LocalState
                    {
                        NodeId = "Node1", // Bu node'un ID'si
                        ProcessCount = 42, // Örnek işlem sayısı
                        QueueMessages = new List<string> { "msg1", "msg2" }
                    };
                    var collection = _mongoDatabase.GetCollection<LocalState>("LocalStates");
                    collection.InsertOne(localState);

                    // Marker'ı diğer node'lara gönder
                    var client = _httpClientFactory.CreateClient();
                    foreach (var node in _otherNodes)
                    {
                        _ = client.PostAsync($"{node}/snapshot/marker", null);
                    }
                }
            }
            return Ok("Marker processed.");
        }

        [HttpPost("register")]
        public IActionResult Register(MemberRegisteredEvent member)
        {
            member.RegisteredAt = DateTime.UtcNow;
            // RabbitMQ'ya event gönder
            var factory = new ConnectionFactory
            {
                HostName = _rabbitMQSettings.HostName,
                UserName = _rabbitMQSettings.UserName,
                Password = _rabbitMQSettings.Password
            };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.QueueDeclare(queue: "member-registered",
                                durable: true,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);
            var message = JsonSerializer.Serialize(member);
            var body = Encoding.UTF8.GetBytes(message);
            channel.BasicPublish(exchange: "",
                                routingKey: "member-registered",
                                basicProperties: null,
                                body: body);
            return Ok(new { Status = "Member registered and event sent", Member = member });
        }
    }
} 