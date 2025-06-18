namespace ServiceDiscovery.Models
{
    public class Node
    {
        public string Id { get; set; }
        public string Address { get; set; }
        public ulong Hash { get; set; }

        public Node(string id, string address)
        {
            Id = id;
            Address = address;
            Hash = CalculateHash(id);
        }

        private ulong CalculateHash(string key)
        {
            ulong hash = 0;
            foreach (char c in key)
            {
                hash = hash * 31 + c;
            }
            return hash;
        }
    }
} 