using BankingApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingApi.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Transaction"/>.
/// </summary>
internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions", table =>
            table.HasCheckConstraint("CK_Transactions_Amount_Positive", "[Amount] > 0"));

        builder.HasKey(transaction => transaction.Id);
        builder.Property(transaction => transaction.Id).ValueGeneratedNever();

        builder.Property(transaction => transaction.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(transaction => transaction.Amount).HasPrecision(18, Account.MoneyDecimalPlaces).IsRequired();
        builder.Property(transaction => transaction.BalanceAfter).HasPrecision(18, Account.MoneyDecimalPlaces).IsRequired();
        builder.Property(transaction => transaction.Reference).HasMaxLength(Transaction.ReferenceMaxLength);
        builder.Property(transaction => transaction.IdempotencyKey).HasMaxLength(Transaction.IdempotencyKeyMaxLength);
        builder.Property(transaction => transaction.CreatedAt).IsRequired();

        // Unique only among rows that have a key; transactions without a key are allowed.
        builder.HasIndex(transaction => transaction.IdempotencyKey)
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL");

        // Supports paging an account's history, newest first.
        builder.HasIndex(transaction => new { transaction.AccountId, transaction.CreatedAt });

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(transaction => transaction.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
