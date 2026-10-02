using FinCore.Auth.Application.Abstractions;
using FinCore.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Auth.Infrastructure.Persistence;

public class AuthDbContext : DbContext, IAuthDbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.HasIndex(x => x.Email).IsUnique(); // aynı e-posta ikinci kez kaydolamaz
            e.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            e.Property(x => x.PasswordHash).IsRequired();
            e.Property(x => x.Role).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.Property(x => x.Token).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Token).IsUnique();
            e.HasOne(x => x.User)
             .WithMany(u => u.RefreshTokens)
             .HasForeignKey(x => x.UserId);
        });
    }
}