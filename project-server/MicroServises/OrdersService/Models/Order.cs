using System;
using System.Collections.Generic;

namespace OrdersService.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending";
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
        public List<OrderItem> Items { get; set; } = new();
    }
}