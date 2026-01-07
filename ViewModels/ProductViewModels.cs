using System.ComponentModel.DataAnnotations;

namespace OnlineShop.ViewModels
{
    public class ProductCreateViewModel
    {
        [Required(ErrorMessage = "Titlul este obligatoriu")]
        [StringLength(200)]
        [Display(Name = "Titlu")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Descrierea este obligatorie")]
        [StringLength(2000)]
        [Display(Name = "Descriere")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Imaginea este obligatorie")]
        [Display(Name = "Imagine")]
        public IFormFile? Image { get; set; }

        [Required(ErrorMessage = "Prețul este obligatoriu")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Prețul trebuie să fie mai mare decât 0")]
        [Display(Name = "Preț")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stocul este obligatoriu")]
        [Range(0, int.MaxValue)]
        [Display(Name = "Stoc")]
        public int Stock { get; set; }

        [Required(ErrorMessage = "Categoria este obligatorie")]
        [Display(Name = "Categorie")]
        public int CategoryId { get; set; }
    }

    public class ProductEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Titlul este obligatoriu")]
        [StringLength(200)]
        [Display(Name = "Titlu")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Descrierea este obligatorie")]
        [StringLength(2000)]
        [Display(Name = "Descriere")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Imagine nouă (opțional)")]
        public IFormFile? NewImage { get; set; }

        public string? CurrentImagePath { get; set; }

        [Required(ErrorMessage = "Prețul este obligatoriu")]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "Preț")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stocul este obligatoriu")]
        [Range(0, int.MaxValue)]
        [Display(Name = "Stoc")]
        public int Stock { get; set; }

        [Required(ErrorMessage = "Categoria este obligatorie")]
        [Display(Name = "Categorie")]
        public int CategoryId { get; set; }
    }

    public class FAQViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Întrebarea este obligatorie")]
        [StringLength(500)]
        [Display(Name = "Întrebare")]
        public string Question { get; set; } = string.Empty;

        [Required(ErrorMessage = "Răspunsul este obligatoriu")]
        [StringLength(1000)]
        [Display(Name = "Răspuns")]
        public string Answer { get; set; } = string.Empty;

        public int ProductId { get; set; }
    }
}
