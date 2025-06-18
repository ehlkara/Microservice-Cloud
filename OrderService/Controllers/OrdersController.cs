using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OrderService.Configuration;
using OrderService.Models;
using Shared.Utils;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IMongoCollection<Order> _orders;
        private readonly RabbitMqSettings _rabbitMQSettings;

        public OrdersController(IOptions<MongoDbSettings> mongoDbSettings, IOptions<RabbitMqSettings> rabbitMQSettings)
        {
            var client = new MongoClient(mongoDbSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoDbSettings.Value.DatabaseName);
            _orders = database.GetCollection<Order>("Orders");
            _rabbitMQSettings = rabbitMQSettings.Value;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _orders.Find(_ => true).ToListAsync();
            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var order = await _orders.Find(o => o.Id == id).FirstOrDefaultAsync();
            if (order == null) return NotFound();
            return Ok(order);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Order order)
        {
            order.CreatedAt = DateTime.UtcNow;
            await _orders.InsertOneAsync(order);

            // RabbitMQ'ya event gönder
            var factory = new ConnectionFactory
            {
                HostName = _rabbitMQSettings.HostName,
                UserName = _rabbitMQSettings.UserName,
                Password = _rabbitMQSettings.Password
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(queue: "order_created",
                                durable: true,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);

            var orderCreatedEvent = new OrderCreatedEvent
            {
                OrderId = order.Id,
                ProductName = order.ProductName,
                Quantity = order.Quantity,
                Price = order.Price,
                CreatedAt = order.CreatedAt
            };

            var message = JsonSerializer.Serialize(orderCreatedEvent);
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: "",
                                routingKey: "order_created",
                                basicProperties: null,
                                body: body);

            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, Order updatedOrder)
        {
            var existingOrder = await _orders.Find(o => o.Id == id).FirstOrDefaultAsync();
            if (existingOrder == null) return NotFound();

            updatedOrder.Id = id;
            updatedOrder.CreatedAt = existingOrder.CreatedAt;

            var result = await _orders.ReplaceOneAsync(o => o.Id == id, updatedOrder);
            if (result.MatchedCount == 0) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _orders.DeleteOneAsync(o => o.Id == id);
            if (result.DeletedCount == 0) return NotFound();
            return NoContent();
        }
    }
} 