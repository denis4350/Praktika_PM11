using Microsoft.EntityFrameworkCore;

namespace HranitelPro.Models
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Request> Requests { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=hranitelpro;Username=postgres;Password=1;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Базовая настройка (остальное уже задано атрибутами)
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Request>().ToTable("requests");
        }
    }
}