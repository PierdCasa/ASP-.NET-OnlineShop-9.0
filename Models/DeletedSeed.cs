using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Models
{

    public class DeletedSeed
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string SeedId { get; set; } = string.Empty;

        public DateTime DeletedAt { get; set; } = DateTime.Now;
    }
}
