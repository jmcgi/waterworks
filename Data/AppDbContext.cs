using Microsoft.EntityFrameworkCore;
using KlaipedosVandenysDemo.Models;

namespace KlaipedosVandenysDemo.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.PersonalCode).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(256).IsRequired();
            entity.Property(u => u.Surname).HasMaxLength(100).IsRequired();
            entity.Property(u => u.Phone).HasMaxLength(50);

            entity.HasIndex(u => u.PersonalCode);
            entity.HasIndex(u => u.Email);
            entity.HasIndex(u => u.Surname);
            entity.HasIndex(u => u.Phone);
        });
    }
}
