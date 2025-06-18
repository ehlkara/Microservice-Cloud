namespace MembershipService.Events
{
    public class MemberRegisteredEvent
    {
        public string MemberId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime RegisteredAt { get; set; }
    }
} 