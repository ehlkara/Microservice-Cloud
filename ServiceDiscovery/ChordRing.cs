using ServiceDiscovery.Interfaces;
using ServiceDiscovery.Models;

namespace ServiceDiscovery
{
    public class ChordRing : IHashRing
    {
        private readonly SortedDictionary<ulong, Node> _nodes;
        private const int RingSize = 1024; // 2^10 nodes

        public ChordRing()
        {
            _nodes = new SortedDictionary<ulong, Node>();
        }

        public void Join(string nodeId, string address)
        {
            var node = new Node(nodeId, address);
            _nodes[node.Hash] = node;
            Console.WriteLine($"Node {nodeId} joined the ring with hash {node.Hash}");
        }

        public void Leave(string nodeId)
        {
            var node = _nodes.Values.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                _nodes.Remove(node.Hash);
                Console.WriteLine($"Node {nodeId} left the ring");
            }
        }

        public string Lookup(string serviceName)
        {
            if (!_nodes.Any())
            {
                throw new InvalidOperationException("No nodes in the ring");
            }

            var serviceHash = new Node(serviceName, "").Hash;
            var node = FindSuccessor(serviceHash);
            return node.Address;
        }

        private Node FindSuccessor(ulong hash)
        {
            // If the hash is greater than all node hashes, wrap around to the first node
            if (hash > _nodes.Keys.Max())
            {
                return _nodes.Values.First();
            }

            // Find the first node with hash greater than or equal to the service hash
            var nodeHash = _nodes.Keys.FirstOrDefault(h => h >= hash);
            if (nodeHash == 0) // If no node found, wrap around
            {
                return _nodes.Values.First();
            }

            return _nodes[nodeHash];
        }

        public IEnumerable<Node> GetNodes()
        {
            return _nodes.Values;
        }
    }
} 