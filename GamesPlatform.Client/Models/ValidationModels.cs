using System.ComponentModel.DataAnnotations;

namespace GamesPlatform.Client.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [MinLength(3, ErrorMessage = "Username must be at least 3 characters")]
        [MaxLength(50, ErrorMessage = "Username cannot exceed 50 characters")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;
    }

    public class CreateGameViewModel
    {
        [Required(ErrorMessage = "Game title is required")]
        [MinLength(1, ErrorMessage = "Game title cannot be empty")]
        [MaxLength(100, ErrorMessage = "Game title cannot exceed 100 characters")]
        public string GameTitle { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Genre is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid genre")]
        public int GenreId { get; set; }

        [Required(ErrorMessage = "Game URL is required")]
        [Url(ErrorMessage = "Invalid URL format")]
        public string GameUrl { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Logo URL cannot exceed 500 characters")]
        public string? Logo { get; set; }

        public DateTime ReleaseDate { get; set; } = DateTime.UtcNow;
    }

    public class UpdateUsernameViewModel
    {
        [Required(ErrorMessage = "New username is required")]
        [MinLength(3, ErrorMessage = "Username must be at least 3 characters")]
        [MaxLength(50, ErrorMessage = "Username cannot exceed 50 characters")]
        public string NewUserName { get; set; } = string.Empty;
    }
}