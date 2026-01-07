using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Models
{
    public class FAQ
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Întrebarea este obligatorie")]
        [StringLength(500, ErrorMessage = "Întrebarea nu poate depăși 500 de caractere")]
        public string Question { get; set; } = string.Empty;

        [Required(ErrorMessage = "Răspunsul este obligatoriu")]
        [StringLength(1000, ErrorMessage = "Răspunsul nu poate depăși 1000 de caractere")]
        public string Answer { get; set; } = string.Empty;

        public int TimesAsked { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }
    }
}
