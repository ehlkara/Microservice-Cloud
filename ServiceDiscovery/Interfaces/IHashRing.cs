using ServiceDiscovery.Models;

namespace ServiceDiscovery.Interfaces
{
    public interface IHashRing
    {
        void Join(string nodeId, string address);
        void Leave(string nodeId);
        string Lookup(string serviceName);
        IEnumerable<Node> GetNodes();
    }
} 