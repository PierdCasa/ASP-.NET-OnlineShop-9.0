using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Models
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<Review>? Reviews { get; set; }
        public virtual ICollection<Order>? Orders { get; set; }
        public virtual ICollection<CartItem>? CartItems { get; set; }
        public virtual ICollection<WishlistItem>? WishlistItems { get; set; }
        public virtual ICollection<Product>? ProposedProducts { get; set; }
    }
}
