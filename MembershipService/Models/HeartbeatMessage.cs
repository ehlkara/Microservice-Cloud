namespace MembershipService.Models
{
    public class HeartbeatMessage
    {
        public string NodeId { get; set; }
        public DateTime Timestamp { get; set; }
        public int QueueDepth { get; set; }
        public double CpuUsage { get; set; }
    }
} 