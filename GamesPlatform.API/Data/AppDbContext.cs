using Microsoft.EntityFrameworkCore;
using GamesPlatform.API.Models;

namespace GamesPlatform.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Game> Games => Set<Game>();
        public DbSet<Genre> Genres => Set<Genre>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<Rating> Ratings => Set<Rating>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Конфигурация User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.UserName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.UserType).HasDefaultValue("User");
            });

            // Конфигурация Game
            modelBuilder.Entity<Game>(entity =>
            {
                entity.HasKey(e => e.GameId);
                entity.Property(e => e.GameTitle).IsRequired().HasMaxLength(200);
                
                entity.HasOne(e => e.Genre)
                      .WithMany(g => g.Games)
                      .HasForeignKey(e => e.GenreId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Developer)
                      .WithMany(u => u.CreatedGames)
                      .HasForeignKey(e => e.DeveloperId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Конфигурация Comment
            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasKey(e => e.CommentId);
                entity.Property(e => e.CommentText).IsRequired();
                
                entity.HasOne(e => e.User)
                      .WithMany(u => u.Comments)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Game)
                      .WithMany(g => g.Comments)
                      .HasForeignKey(e => e.GameId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Конфигурация Rating
            modelBuilder.Entity<Rating>(entity =>
            {
                entity.HasKey(e => e.RatingId);
                entity.Property(e => e.RatingValue).IsRequired();
                
                // Один пользователь может оценить игру только один раз
                entity.HasIndex(e => new { e.UserId, e.GameId }).IsUnique();

                entity.HasOne(e => e.User)
                      .WithMany(u => u.Ratings)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Game)
                      .WithMany(g => g.Ratings)
                      .HasForeignKey(e => e.GameId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}