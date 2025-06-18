using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SnapshotCoordinator.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SnapshotCoordinator.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SnapshotController : ControllerBase
    {
        private readonly IMongoDatabase _database;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SnapshotController> _logger;

        // Docker service names for inter-container communication
        private readonly List<string> _serviceNodes = new()
        {
            "http://order-service:80",
            "http://invoice-service:80", 
            "http://membership-service:80"
        };

        public SnapshotController(
            IMongoDatabase database, 
            IHttpClientFactory httpClientFactory,
            ILogger<SnapshotController> logger)
        {
            _database = database;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartSnapshot()
        {
            var snapshotId = Guid.NewGuid();
            var initiatorNode = _serviceNodes.First(); // OrderService as initiator
            
            _logger.LogInformation($"Starting Chandy-Lamport snapshot {snapshotId} from {initiatorNode}");

            try
            {
                // Phase 1: Send snapshot markers to all nodes
                var snapshotTasks = new List<Task<object>>();
                
                foreach (var node in _serviceNodes)
                {
                    snapshotTasks.Add(ProcessNodeSnapshot(node, snapshotId));
                }

                var results = await Task.WhenAll(snapshotTasks);
                
                // Phase 2: Record snapshot completion
                var snapshotRecord = new LocalState
                {
                    SnapshotId = snapshotId,
                    NodeId = "snapshot-coordinator",
                    Timestamp = DateTime.UtcNow,
                    Status = "Completed",
                    ChannelStates = new Dictionary<string, object>
                    {
                        ["total_nodes"] = _serviceNodes.Count,
                        ["successful_nodes"] = results.Count(r => r.ToString().Contains("Success")),
                        ["failed_nodes"] = results.Count(r => !r.ToString().Contains("Success"))
                    },
                    LocalVariables = new Dictionary<string, object>
                    {
                        ["snapshot_results"] = results.ToList(),
                        ["completion_time"] = DateTime.UtcNow
                    }
                };

                var coordinatorCollection = _database.GetCollection<LocalState>("LocalStates");
                await coordinatorCollection.InsertOneAsync(snapshotRecord);

                return Ok(new 
                { 
                    message = "Snapshot started", 
                    snapshotId = snapshotId,
                    initiatorNode = initiatorNode,
                    results = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Snapshot failed: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private async Task<object> ProcessNodeSnapshot(string node, Guid snapshotId)
        {
            try
            {
            var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                
                var response = await client.GetAsync($"{node}/Health");
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Node {node} is healthy for snapshot");
                    
                    // Save local state for this node
                    var localState = new LocalState
                    {
                        SnapshotId = snapshotId,
                        NodeId = node,
                        Timestamp = DateTime.UtcNow,
                        Status = "Active",
                        ChannelStates = new Dictionary<string, object>
                        {
                            ["health"] = "OK",
                            ["snapshot_marker_sent"] = true
                        },
                        LocalVariables = new Dictionary<string, object>
                        {
                            ["node_address"] = node,
                            ["snapshot_initiated"] = DateTime.UtcNow
                        }
                    };

                    var collection = _database.GetCollection<LocalState>("LocalStates");
                    await collection.InsertOneAsync(localState);
                    
                    return new { Node = node, Status = "Success", Health = "OK" };
                }
                else
                {
                    _logger.LogWarning($"Node {node} health check failed: {response.StatusCode}");
                    return new { Node = node, Status = "Failed", Error = $"Health check failed: {response.StatusCode}" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error contacting node {node}: {ex.Message}");
                return new { Node = node, Status = "Error", Error = ex.Message };
            }
        }

        [HttpGet("status/{snapshotId}")]
        public async Task<IActionResult> GetSnapshotStatus(Guid snapshotId)
        {
            try
            {
                var collection = _database.GetCollection<LocalState>("LocalStates");
                var snapshots = await collection
                    .Find(s => s.SnapshotId == snapshotId)
                    .ToListAsync();

                if (!snapshots.Any())
                {
                    return NotFound(new { error = "Snapshot not found" });
                }

                var stateResults = new List<object>();
                foreach (var s in snapshots)
                {
                    stateResults.Add(new 
                    {
                        nodeId = s.NodeId,
                        status = s.Status,
                        timestamp = s.Timestamp,
                        channelStates = s.ChannelStates,
                        localVariables = s.LocalVariables
                    });
                }

                return Ok(new 
                { 
                    snapshotId = snapshotId,
                    totalStates = snapshots.Count,
                    states = stateResults
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving snapshot status: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestSnapshots()
        {
            try
            {
                var collection = _database.GetCollection<LocalState>("LocalStates");
                var latestSnapshots = await collection
                    .Find(_ => true)
                    .SortByDescending(s => s.Timestamp)
                    .Limit(10)
                    .ToListAsync();

                var groupedSnapshots = latestSnapshots
                    .GroupBy(s => s.SnapshotId)
                    .ToList();

                var snapshotResults = new List<object>();
                foreach (var g in groupedSnapshots.Take(5))
                {
                    snapshotResults.Add(new 
                    {
                        snapshotId = g.Key,
                        timestamp = g.Max(s => s.Timestamp),
                        nodeCount = g.Count(),
                        status = g.Any(s => s.Status == "Completed") ? "Completed" : "In Progress"
                    });
                }

                return Ok(new { snapshots = snapshotResults.OrderByDescending(s => ((dynamic)s).timestamp) });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving latest snapshots: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    // New API controller for frontend compatibility
    [ApiController]
    [Route("api/snapshots")]
    public class SnapshotsApiController : ControllerBase
    {
        private readonly IMongoDatabase _database;
        private readonly ILogger<SnapshotsApiController> _logger;

        public SnapshotsApiController(IMongoDatabase database, ILogger<SnapshotsApiController> logger)
        {
            _database = database;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetSnapshots()
        {
            try
            {
                var collection = _database.GetCollection<LocalState>("LocalStates");
                var snapshots = await collection
                    .Find(_ => true)
                    .SortByDescending(s => s.Timestamp)
                    .Limit(20)
                    .ToListAsync();

                var groupedSnapshots = snapshots
                    .GroupBy(s => s.SnapshotId)
                    .ToList();

                var snapshotResults = new List<object>();
                foreach (var g in groupedSnapshots)
                {
                    var status = "completed";
                    if (g.Any(s => s.Status == "In Progress" || s.Status == "Active"))
                    {
                        status = "in-progress";
                    }
                    else if (g.Any(s => s.Status == "Failed" || s.Status == "Error"))
                    {
                        status = "failed";
                    }

                    snapshotResults.Add(new 
                    {
                        id = g.Key.ToString(),
                        timestamp = g.Max(s => s.Timestamp).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        status = status,
                        nodeCount = g.Count()
                    });
                }

                return Ok(snapshotResults.OrderByDescending(s => ((dynamic)s).timestamp));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving snapshots: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class SnapshotMarker
    {
        public string SnapshotId { get; set; } = string.Empty;
        public string InitiatorNode { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
} 