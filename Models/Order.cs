using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineShop.Models
{
    public enum OrderStatus
    {
        Plasata,
        Livrata,
        Anulata
    }

    public class Order
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public string Status { get; set; } = "Plasata";

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(500)]
        public string? ShippingAddress { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser? User { get; set; }

        public virtual ICollection<OrderItem>? OrderItems { get; set; }

        public void CalculateTotal()
        {
            if (OrderItems != null && OrderItems.Any())
            {
                TotalAmount = OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice);
            }
        }
    }
}
