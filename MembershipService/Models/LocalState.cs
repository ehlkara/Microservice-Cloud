namespace MembershipService.Models
{
    public class LocalState
    {
        public string NodeId { get; set; }
        public int ProcessCount { get; set; }
        public List<string> QueueMessages { get; set; } = new();
    }
} 