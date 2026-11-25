using BankingApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingApi.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="User"/>.
/// </summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();

        builder.Property(user => user.Email).HasMaxLength(User.EmailMaxLength).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(User.PasswordHashMaxLength).IsRequired();
        builder.Property(user => user.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(user => user.CreatedAt).IsRequired();

        builder.HasIndex(user => user.Email).IsUnique();

        // One login per customer.
        builder.HasIndex(user => user.CustomerId).IsUnique().HasFilter("[CustomerId] IS NOT NULL");

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(user => user.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
