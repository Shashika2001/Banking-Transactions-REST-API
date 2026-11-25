using BankingApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingApi.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="AuditLog"/>.
/// </summary>
internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(auditLog => auditLog.Id);
        builder.Property(auditLog => auditLog.Id).ValueGeneratedNever();

        builder.Property(auditLog => auditLog.UserId).HasMaxLength(AuditLog.UserIdMaxLength).IsRequired();
        builder.Property(auditLog => auditLog.Action).HasMaxLength(AuditLog.ActionMaxLength).IsRequired();
        builder.Property(auditLog => auditLog.EntityName).HasMaxLength(AuditLog.EntityNameMaxLength).IsRequired();
        builder.Property(auditLog => auditLog.EntityId).HasMaxLength(AuditLog.EntityIdMaxLength).IsRequired();
        builder.Property(auditLog => auditLog.Details).HasMaxLength(AuditLog.DetailsMaxLength);
        builder.Property(auditLog => auditLog.Timestamp).IsRequired();

        builder.HasIndex(auditLog => new { auditLog.EntityName, auditLog.EntityId });
        builder.HasIndex(auditLog => auditLog.Timestamp);
    }
}
