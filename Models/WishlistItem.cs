using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Models
{
    public class WishlistItem
    {
        public int Id { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.Now;

        [Required]
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser? User { get; set; }

        [Required]
        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }
    }
}
