using System.ComponentModel.DataAnnotations;

namespace OnlineShop.ViewModels
{
    public class ProfileViewModel
    {
        public string Email { get; set; } = "";

        [Required]
        public string FirstName { get; set; } = "";

        [Required]
        public string LastName { get; set; } = "";
    }
}
