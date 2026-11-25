using BankingApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingApi.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Customer"/>.
/// </summary>
internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(customer => customer.Id);
        builder.Property(customer => customer.Id).ValueGeneratedNever();

        builder.Property(customer => customer.FullName).HasMaxLength(Customer.FullNameMaxLength).IsRequired();
        builder.Property(customer => customer.Email).HasMaxLength(Customer.EmailMaxLength).IsRequired();
        builder.Property(customer => customer.NationalId).HasMaxLength(Customer.NationalIdMaxLength).IsRequired();
        builder.Property(customer => customer.CreatedAt).IsRequired();

        builder.HasIndex(customer => customer.Email).IsUnique();
        builder.HasIndex(customer => customer.NationalId).IsUnique();
    }
}
