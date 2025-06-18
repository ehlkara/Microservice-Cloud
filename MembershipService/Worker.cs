using MembershipService.Models;
using MembershipService.Services;

namespace MembershipService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly InMemoryMembership _membership;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _nodeId = Guid.NewGuid().ToString();
    private readonly List<string> _neighbors = new() {
        "http://localhost:5001/gossip",
        "http://localhost:5002/gossip",
        "http://localhost:5003/gossip"
    };
    private readonly Random _rnd = new();

    public Worker(ILogger<Worker> logger, InMemoryMembership membership, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _membership = membership;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var msg = new HeartbeatMessage
            {
                NodeId = _nodeId,
                Timestamp = DateTime.UtcNow,
                QueueDepth = _rnd.Next(0, 100),
                CpuUsage = _rnd.NextDouble() * 100
            };

            // Rastgele 3 komşuya gönder
            var selected = _neighbors.OrderBy(x => _rnd.Next()).Take(3).ToList();
            foreach (var url in selected)
            {
                try
                {
                    var client = _httpClientFactory.CreateClient();
                    await client.PostAsJsonAsync(url, msg, cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to send heartbeat to {url}: {ex.Message}");
                }
            }

            // Kendi membership listesine de ekle
            _membership.Update(msg);

            // 30 saniyeden uzun süredir heartbeat gelmeyenleri failed olarak işaretle
            _membership.MarkFailedNodes();

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
