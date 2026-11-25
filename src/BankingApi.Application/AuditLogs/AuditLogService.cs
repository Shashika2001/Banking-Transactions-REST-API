using BankingApi.Application.Abstractions.Persistence;
using BankingApi.Application.Common.Models;
using BankingApi.Application.Common.Security;
using FluentValidation;

namespace BankingApi.Application.AuditLogs;

/// <summary>
/// Default implementation of <see cref="IAuditLogService"/>.
/// </summary>
public sealed class AuditLogService : IAuditLogService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccessPolicy _accessPolicy;
    private readonly IValidator<AuditLogQuery> _queryValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLogService"/> class.
    /// </summary>
    public AuditLogService(IUnitOfWork unitOfWork, IAccessPolicy accessPolicy, IValidator<AuditLogQuery> queryValidator)
    {
        _unitOfWork = unitOfWork;
        _accessPolicy = accessPolicy;
        _queryValidator = queryValidator;
    }

    /// <inheritdoc />
    public async Task<PagedResult<AuditLogResponse>> SearchAsync(AuditLogQuery query, CancellationToken cancellationToken = default)
    {
        _accessPolicy.EnsureAdmin();
        await _queryValidator.ValidateAndThrowAsync(query, cancellationToken);

        var page = await _unitOfWork.AuditLogs.SearchAsync(query, cancellationToken);
        return page.Map(entry => new AuditLogResponse(
            entry.Id,
            entry.UserId,
            entry.Action,
            entry.EntityName,
            entry.EntityId,
            entry.Details,
            entry.Timestamp));
    }
}
