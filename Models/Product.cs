using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineShop.Models
{
    public enum ProductStatus
    {
        Pending,
        Aprobat,
        Respins
    }

    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Titlul este obligatoriu")]
        [StringLength(200, ErrorMessage = "Titlul nu poate depăși 200 de caractere")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Descrierea este obligatorie")]
        [StringLength(2000, ErrorMessage = "Descrierea nu poate depăși 2000 de caractere")]
        public string Description { get; set; } = string.Empty;

        public string? ImagePath { get; set; }

        [Required(ErrorMessage = "Prețul este obligatoriu")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Prețul trebuie să fie mai mare decât 0")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stocul este obligatoriu")]
        [Range(1, int.MaxValue, ErrorMessage = "Stocul trebuie să fie cel puțin 1")]
        public int Stock { get; set; }

        [Column(TypeName = "decimal(3,2)")]
        public decimal? Rating { get; set; }

        public ProductStatus Status { get; set; } = ProductStatus.Pending;

        [StringLength(500)]
        public string? AdminFeedback { get; set; }

        [StringLength(500)]
        public string? RejectionReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        [StringLength(100)]
        public string? SeedId { get; set; }

        [Required]
        public int CategoryId { get; set; }
        public virtual Category? Category { get; set; }

        public string? CreatedByUserId { get; set; }
        public virtual ApplicationUser? CreatedByUser { get; set; }

        public virtual ICollection<Review>? Reviews { get; set; }
        public virtual ICollection<CartItem>? CartItems { get; set; }
        public virtual ICollection<WishlistItem>? WishlistItems { get; set; }
        public virtual ICollection<OrderItem>? OrderItems { get; set; }
        public virtual ICollection<FAQ>? FAQs { get; set; }

        public void CalculateAverageRating()
        {
            if (Reviews != null && Reviews.Any(r => r.Rating.HasValue))
            {
                var ratingsWithValue = Reviews.Where(r => r.Rating.HasValue).Select(r => r.Rating!.Value);
                Rating = (decimal)ratingsWithValue.Average();
            }
            else
            {
                Rating = null;
            }
        }
    }
}
