using BankingApi.Application.Abstractions.Persistence;
using BankingApi.Application.AuditLogs;
using BankingApi.Application.Common.Models;
using BankingApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAuditLogRepository"/>.
/// </summary>
internal sealed class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLogRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public AuditLogRepository(BankingDbContext dbContext)
        : base(dbContext)
    {
    }

    /// <inheritdoc />
    public async Task<PagedResult<AuditLog>> SearchAsync(AuditLogQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfLessThan(query.PageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(query.PageSize, 1);

        var entries = Set.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.UserId))
        {
            entries = entries.Where(entry => entry.UserId == query.UserId);
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            entries = entries.Where(entry => entry.Action == query.Action);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityName))
        {
            entries = entries.Where(entry => entry.EntityName == query.EntityName);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            entries = entries.Where(entry => entry.EntityId == query.EntityId);
        }

        if (query.From.HasValue)
        {
            entries = entries.Where(entry => entry.Timestamp >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            entries = entries.Where(entry => entry.Timestamp < query.To.Value);
        }

        var totalCount = await entries.CountAsync(cancellationToken);
        var items = await entries
            .OrderByDescending(entry => entry.Timestamp)
            .ThenByDescending(entry => entry.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLog>(items, query.PageNumber, query.PageSize, totalCount);
    }
}
