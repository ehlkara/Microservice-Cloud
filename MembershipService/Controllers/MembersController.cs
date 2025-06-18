using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MembershipService.Models;
using Microsoft.Extensions.Options;
using MembershipService.Configuration;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using MembershipService.Services;

namespace MembershipService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MembersController : ControllerBase
    {
        private readonly IMongoCollection<Member> _members;
        private readonly RabbitMqSettings _rabbitMQSettings;
        private readonly ILogger<MembersController> _logger;
        private readonly InMemoryMembership _membership;

        public MembersController(
            IOptions<MongoDbSettings> mongoDbSettings,
            IOptions<RabbitMqSettings> rabbitMQSettings,
            ILogger<MembersController> logger,
            InMemoryMembership membership)
        {
            var client = new MongoClient(mongoDbSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoDbSettings.Value.DatabaseName);
            _members = database.GetCollection<Member>("Members");
            _rabbitMQSettings = rabbitMQSettings.Value;
            _logger = logger;
            _membership = membership;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var members = await _members.Find(_ => true).ToListAsync();
            return Ok(members);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var member = await _members.Find(m => m.Id == id).FirstOrDefaultAsync();
            if (member == null) return NotFound();
            return Ok(member);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Member member)
        {
            member.CreatedAt = DateTime.UtcNow;
            await _members.InsertOneAsync(member);

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

            var memberRegisteredEvent = new Events.MemberRegisteredEvent
            {
                MemberId = member.Id,
                Name = member.Name,
                Email = member.Email,
                RegisteredAt = member.CreatedAt
            };

            var message = JsonSerializer.Serialize(memberRegisteredEvent);
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: "",
                                routingKey: "member-registered",
                                basicProperties: null,
                                body: body);

            _logger.LogInformation($"Member registered: {member.Name} ({member.Email})");

            return CreatedAtAction(nameof(GetById), new { id = member.Id }, member);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, Member updatedMember)
        {
            var existingMember = await _members.Find(m => m.Id == id).FirstOrDefaultAsync();
            if (existingMember == null) return NotFound();

            updatedMember.Id = id;
            updatedMember.CreatedAt = existingMember.CreatedAt;

            var result = await _members.ReplaceOneAsync(m => m.Id == id, updatedMember);
            if (result.MatchedCount == 0) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _members.DeleteOneAsync(m => m.Id == id);
            if (result.DeletedCount == 0) return NotFound();
            return NoContent();
        }
    }

    // New controller for DHT operations
    [ApiController]
    [Route("api/dht")]
    public class DhtController : ControllerBase
    {
        private static readonly List<DhtNode> _dhtNodes = new List<DhtNode>();

        public class DhtNode
        {
            public string Id { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string Status { get; set; } = "active";
            public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
            public int HashValue { get; set; }
        }

        [HttpGet("nodes")]
        public ActionResult<IEnumerable<DhtNode>> GetNodes()
        {
            return Ok(_dhtNodes);
        }

        [HttpPost("add-node")]
        public ActionResult<DhtNode> AddNode([FromBody] DhtNode nodeRequest)
        {
            var node = new DhtNode
            {
                Id = nodeRequest.Id ?? $"node-{Guid.NewGuid().ToString()[..8]}",
                Address = nodeRequest.Address ?? $"192.168.1.{new Random().Next(1, 255)}:{5000 + new Random().Next(0, 100)}",
                Status = "active",
                JoinedAt = DateTime.UtcNow,
                HashValue = Math.Abs(nodeRequest.Id?.GetHashCode() ?? new Random().Next()) % 1000
            };

            _dhtNodes.Add(node);
            return Ok(node);
        }

        [HttpDelete("nodes/{id}")]
        public ActionResult RemoveNode(string id)
        {
            var node = _dhtNodes.FirstOrDefault(n => n.Id == id);
            if (node == null)
            {
                return NotFound();
            }

            node.Status = "inactive";
            return NoContent();
        }

        [HttpGet("lookup/{key}")]
        public ActionResult<DhtNode> LookupNode(string key)
        {
            if (!_dhtNodes.Any())
            {
                return NotFound("No nodes available in DHT");
            }

            var keyHash = Math.Abs(key.GetHashCode()) % 1000;
            var activeNodes = _dhtNodes.Where(n => n.Status == "active").OrderBy(n => n.HashValue).ToList();
            
            if (!activeNodes.Any())
            {
                return NotFound("No active nodes available");
            }

            // Find the node responsible for this key (consistent hashing)
            var responsibleNode = activeNodes.FirstOrDefault(n => n.HashValue >= keyHash) ?? activeNodes.First();
            
            return Ok(responsibleNode);
        }
    }

    // New controller for Gossip protocol
    [ApiController]
    [Route("api/gossip")]
    public class GossipController : ControllerBase
    {
        private static readonly List<GossipMessage> _gossipMessages = new List<GossipMessage>();
        private static readonly List<GossipNode> _gossipNodes = new List<GossipNode>();

        public class GossipMessage
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string NodeId { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; } = DateTime.UtcNow;
            public int Ttl { get; set; } = 3; // Time to live for gossip propagation
        }

        public class GossipNode
        {
            public string Id { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string Status { get; set; } = "active";
            public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
        }

        [HttpGet("messages")]
        public ActionResult<IEnumerable<GossipMessage>> GetMessages()
        {
            return Ok(_gossipMessages.OrderByDescending(m => m.Timestamp).Take(50));
        }

        [HttpPost("send")]
        public ActionResult<GossipMessage> SendMessage([FromBody] GossipMessage messageRequest)
        {
            var message = new GossipMessage
            {
                NodeId = messageRequest.NodeId ?? $"node-{Guid.NewGuid().ToString()[..8]}",
                Message = messageRequest.Message ?? "Gossip message",
                Timestamp = DateTime.UtcNow,
                Ttl = messageRequest.Ttl > 0 ? messageRequest.Ttl : 3
            };

            _gossipMessages.Add(message);

            // Add/update the node in gossip membership
            var existingNode = _gossipNodes.FirstOrDefault(n => n.Id == message.NodeId);
            if (existingNode != null)
            {
                existingNode.LastHeartbeat = DateTime.UtcNow;
                existingNode.Status = "active";
            }
            else
            {
                _gossipNodes.Add(new GossipNode
                {
                    Id = message.NodeId,
                    Address = $"192.168.1.{new Random().Next(1, 255)}:{5000 + new Random().Next(0, 100)}",
                    Status = "active",
                    LastHeartbeat = DateTime.UtcNow
                });
            }

            // Simulate gossip propagation (in real implementation, this would propagate to other nodes)
            PropagateGossip(message);

            return Ok(message);
        }

        private void PropagateGossip(GossipMessage message)
        {
            // Simulate gossip propagation to other nodes
            if (message.Ttl > 0)
            {
                var activeNodes = _gossipNodes.Where(n => n.Status == "active" && n.Id != message.NodeId).ToList();
                var propagationCount = Math.Min(2, activeNodes.Count); // Gossip to 2 random nodes

                for (int i = 0; i < propagationCount; i++)
                {
                    var targetNode = activeNodes[new Random().Next(activeNodes.Count)];
                    // In real implementation, would send HTTP request to target node
                    // For simulation, we just log the propagation
                    Console.WriteLine($"Gossip propagated from {message.NodeId} to {targetNode.Id}");
                }
            }
        }

        [HttpGet("nodes")]
        public ActionResult<IEnumerable<GossipNode>> GetGossipNodes()
        {
            // Mark nodes as inactive if they haven't sent heartbeat in last 30 seconds
            var cutoffTime = DateTime.UtcNow.AddSeconds(-30);
            foreach (var node in _gossipNodes.Where(n => n.LastHeartbeat < cutoffTime))
            {
                node.Status = "inactive";
            }

            return Ok(_gossipNodes);
        }
    }

    // Update membership endpoint
    [ApiController]
    [Route("api/membership")]
    public class MembershipController : ControllerBase
    {
        private readonly IMongoCollection<Member> _members;

        public MembershipController(IOptions<MongoDbSettings> mongoDbSettings)
        {
            var client = new MongoClient(mongoDbSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoDbSettings.Value.DatabaseName);
            _members = database.GetCollection<Member>("Members");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetMembership()
        {
            var members = await _members.Find(_ => true).ToListAsync();
            var membershipNodes = members.Select(m => new
            {
                id = m.Id,
                address = $"{m.Name}@{m.Email}",
                status = "active" // Assuming all members are active for now
            });

            return Ok(membershipNodes);
        }
    }
} 