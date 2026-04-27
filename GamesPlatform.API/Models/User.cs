namespace GamesPlatform.API.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string UserType { get; set; } = "User"; // "User" или "Admin"
        public bool IsActive { get; set; } = true;
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        // Навигационные свойства
        public ICollection<Game> CreatedGames { get; set; } = new List<Game>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    }
}