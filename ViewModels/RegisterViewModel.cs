using System.ComponentModel.DataAnnotations;

namespace OnlineShop.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Prenumele este obligatoriu")]
        public string FirstName { get; set; } = "";

        [Required(ErrorMessage = "Numele este obligatoriu")]
        public string LastName { get; set; } = "";

        [Required(ErrorMessage = "Email-ul este obligatoriu")]
        [EmailAddress(ErrorMessage = "Email invalid")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Parola este obligatorie")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Parola trebuie să aibă cel puțin 6 caractere")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Parolele nu se potrivesc")]
        public string ConfirmPassword { get; set; } = "";
    }
}
