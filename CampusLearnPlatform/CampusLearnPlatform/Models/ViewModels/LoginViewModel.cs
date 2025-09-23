using System.ComponentModel.DataAnnotations;

namespace CampusLearnPlatform.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [RegularExpression(@"[0-9]{6}@student\.belgiumcampus\.ac\.za$",
            ErrorMessage = "Please use your Belgium Campus email address - Login")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
