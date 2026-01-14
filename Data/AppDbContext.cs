using Microsoft.EntityFrameworkCore;
using KlaipedosVandenysDemo.Models;

namespace KlaipedosVandenysDemo.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserIdentifier> UserIdentifiers => Set<UserIdentifier>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<Meter> Meters => Set<Meter>();
    public DbSet<Incident> Incidents => Set<Incident>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            // Postgres lower-cases unquoted identifiers, so a table created as users
            // is not the same as a quoted "Users".
            entity.ToTable("users");
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Id).HasColumnName("id");
            entity.Property(u => u.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(u => u.Surname).HasColumnName("surname").HasMaxLength(100).IsRequired();
            entity.Property(u => u.PersonalCode).HasColumnName("personalcode").IsRequired();
            entity.Property(u => u.Email).HasColumnName("email").HasMaxLength(256).IsRequired();
            entity.Property(u => u.Phone).HasColumnName("phone").HasMaxLength(50);

            entity.HasIndex(u => u.PersonalCode);
            entity.HasIndex(u => u.Email);
            entity.HasIndex(u => u.Surname);
            entity.HasIndex(u => u.Phone);
        });

        modelBuilder.Entity<UserIdentifier>(entity =>
        {
            entity.ToTable("user_identifiers");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("userid");
            entity.Property(x => x.Type).HasColumnName("type").HasMaxLength(50).IsRequired();
            entity.Property(x => x.Value).HasColumnName("value").HasMaxLength(64).IsRequired();

            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => new { x.Type, x.Value }).IsUnique();

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId);
        });
        
        modelBuilder.Entity<Bill>(entity =>
        {
            entity.ToTable("bills");
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Id).HasColumnName("id");
            entity.Property(b => b.UserId).HasColumnName("userid");
            entity.Property(b => b.Amount).HasColumnName("amount");
            entity.Property(b => b.Status).HasColumnName("status");
        });
        
        modelBuilder.Entity<Meter>(entity =>
        {
            entity.ToTable("meters");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Id).HasColumnName("id");
            entity.Property(m => m.MeterType).HasColumnName("metertype");
            entity.Property(m => m.Value).HasColumnName("value");
            entity.Property(m => m.UserId).HasColumnName("userid");
            entity.Property(m => m.LastUpdated).HasColumnName("lastupdated");
        });

        modelBuilder.Entity<Incident>(entity =>
        {
            entity.ToTable("incidents");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Id).HasColumnName("id");
            entity.Property(i => i.UserId).HasColumnName("userid");
            entity.Property(i => i.Description).HasColumnName("description").HasMaxLength(1000).IsRequired();
            entity.Property(i => i.CreatedAt).HasColumnName("createdat").IsRequired();
            entity.Property(i => i.Status).HasColumnName("status").HasMaxLength(50).IsRequired();

            entity.HasIndex(i => i.UserId);
            entity.HasOne(i => i.User)
                .WithMany()
                .HasForeignKey(i => i.UserId);
        });
    }
}
