using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shop.Domain.Entities
{
    public class Order
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }
        public Guid? AccountId { get; set; } // Only linked to the authenticated account at checkout.

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "Pending";

        public decimal TotalAmount { get; set; }

        // An unpaid checkout is NOT a paid/fulfilled order.
        public Guid? SourceCartId { get; set; }
        public string ShippingCity { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string? ShippingPostalCode { get; set; }

        public Customer Customer { get; set; } = null!;

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
