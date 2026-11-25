using BankingApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingApi.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Account"/>.
/// </summary>
internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts", table =>
            table.HasCheckConstraint("CK_Accounts_Balance_NonNegative", "[Balance] >= 0"));

        builder.HasKey(account => account.Id);
        builder.Property(account => account.Id).ValueGeneratedNever();

        builder.Property(account => account.AccountNumber).HasMaxLength(Account.AccountNumberMaxLength).IsRequired();
        builder.Property(account => account.Balance).HasPrecision(18, Account.MoneyDecimalPlaces).IsRequired();
        builder.Property(account => account.Currency)
            .HasMaxLength(Account.CurrencyCodeLength)
            .IsFixedLength()
            .IsUnicode(false)
            .IsRequired();
        builder.Property(account => account.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(account => account.RowVersion).IsRowVersion();
        builder.Property(account => account.CreatedAt).IsRequired();

        builder.HasIndex(account => account.AccountNumber).IsUnique();
        builder.HasIndex(account => account.CustomerId);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(account => account.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
