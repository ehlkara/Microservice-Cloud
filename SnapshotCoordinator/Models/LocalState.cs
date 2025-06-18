namespace SnapshotCoordinator.Models
{
    public class LocalState
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string NodeId { get; set; } = string.Empty;
        public Guid SnapshotId { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ProcessCount { get; set; }
        public List<string> QueueMessages { get; set; } = new List<string>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> ChannelStates { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> LocalVariables { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> AdditionalState { get; set; } = new Dictionary<string, object>();
    }
} 