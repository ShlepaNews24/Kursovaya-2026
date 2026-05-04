// Модель пользователя с поддержкой профиля и связей
using System.ComponentModel.DataAnnotations;

namespace GamesPlatform.API.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required, MaxLength(50)]
        public string UserName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string UserType { get; set; } = "User";

        public bool IsActive { get; set; } = true;

        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginDate { get; set; }
        public DateTime? DateOfBirth { get; set; }

        public ICollection<Game>? Games { get; set; } = new List<Game>();
        public ICollection<Comment>? Comments { get; set; } = new List<Comment>();
        public ICollection<Rating>? Ratings { get; set; } = new List<Rating>();
    }
}