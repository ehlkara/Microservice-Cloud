using MembershipService.Models;
using System.Collections.Concurrent;

namespace MembershipService.Services
{
    public class InMemoryMembership
    {
        private readonly ConcurrentDictionary<string, (HeartbeatMessage Message, DateTime LastSeen, bool Failed)> _nodes = new();

        public void Update(HeartbeatMessage msg)
        {
            _nodes.AddOrUpdate(msg.NodeId,
                (msg, DateTime.UtcNow, false),
                (key, old) => (msg, DateTime.UtcNow, false));
        }

        public void MarkFailedNodes()
        {
            var now = DateTime.UtcNow;
            foreach (var key in _nodes.Keys)
            {
                var node = _nodes[key];
                if (!node.Failed && (now - node.LastSeen).TotalSeconds > 30)
                {
                    _nodes[key] = (node.Message, node.LastSeen, true);
                }
            }
        }

        public IEnumerable<(HeartbeatMessage Message, DateTime LastSeen, bool Failed)> GetAll() => _nodes.Values;
    }
} 