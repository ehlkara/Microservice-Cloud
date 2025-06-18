namespace OrderService.Models
{
    public class InvoiceRequestDto
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; }
        public decimal Amount { get; set; }
        public DateTime RequestDate { get; set; }
    }
} 