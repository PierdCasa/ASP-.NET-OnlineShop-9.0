using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Models
{
    public class Review
    {
        public int Id { get; set; }

        [StringLength(1000, ErrorMessage = "Review-ul nu poate depăși 1000 de caractere")]
        public string? Text { get; set; }

        [Range(1, 5, ErrorMessage = "Rating-ul trebuie să fie între 1 și 5")]
        public int? Rating { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        [Required]
        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser? User { get; set; }
    }
}
