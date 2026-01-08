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
            // Postgres lower-cases unquoted identifiers, so a table created as users
            // is not the same as a quoted "Users".
            entity.ToTable("users");
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Id).HasColumnName("id");
            entity.Property(u => u.PersonalCode).HasColumnName("personalcode").IsRequired();
            entity.Property(u => u.Email).HasColumnName("email").HasMaxLength(256).IsRequired();
            entity.Property(u => u.Surname).HasColumnName("surname").HasMaxLength(100).IsRequired();
            entity.Property(u => u.Phone).HasColumnName("phone").HasMaxLength(50);

            entity.HasIndex(u => u.PersonalCode);
            entity.HasIndex(u => u.Email);
            entity.HasIndex(u => u.Surname);
            entity.HasIndex(u => u.Phone);
        });
    }
}
