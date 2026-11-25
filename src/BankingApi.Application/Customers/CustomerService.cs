using BankingApi.Application.Abstractions.Persistence;
using BankingApi.Application.Auditing;
using BankingApi.Application.Common.Exceptions;
using BankingApi.Application.Common.Models;
using BankingApi.Domain.Entities;
using FluentValidation;

namespace BankingApi.Application.Customers;

/// <summary>
/// Default implementation of <see cref="ICustomerService"/>.
/// </summary>
public sealed class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditTrail _auditTrail;
    private readonly TimeProvider _timeProvider;
    private readonly IValidator<CreateCustomerRequest> _createValidator;
    private readonly IValidator<PageRequest> _pageValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerService"/> class.
    /// </summary>
    public CustomerService(
        IUnitOfWork unitOfWork,
        IAuditTrail auditTrail,
        TimeProvider timeProvider,
        IValidator<CreateCustomerRequest> createValidator,
        IValidator<PageRequest> pageValidator)
    {
        _unitOfWork = unitOfWork;
        _auditTrail = auditTrail;
        _timeProvider = timeProvider;
        _createValidator = createValidator;
        _pageValidator = pageValidator;
    }

    /// <inheritdoc />
    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var email = request.Email.Trim();
        var nationalId = request.NationalId.Trim();

        if (await _unitOfWork.Customers.EmailExistsAsync(email, cancellationToken))
        {
            throw new ConflictException($"A customer with email '{email}' already exists.");
        }

        if (await _unitOfWork.Customers.NationalIdExistsAsync(nationalId, cancellationToken))
        {
            throw new ConflictException("A customer with this national ID already exists.");
        }

        var customer = Customer.Create(request.FullName, email, nationalId, _timeProvider.GetUtcNow());

        await _unitOfWork.Customers.AddAsync(customer, cancellationToken);
        await _auditTrail.RecordAsync(AuditActions.CustomerCreated, nameof(Customer), customer.Id, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return customer.ToResponse();
    }

    /// <inheritdoc />
    public async Task<CustomerResponse> GetByIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), customerId);

        return customer.ToResponse();
    }

    /// <inheritdoc />
    public async Task<PagedResult<CustomerResponse>> ListAsync(PageRequest request, CancellationToken cancellationToken = default)
    {
        await _pageValidator.ValidateAndThrowAsync(request, cancellationToken);

        var page = await _unitOfWork.Customers.ListAsync(request.PageNumber, request.PageSize, cancellationToken);
        return page.Map(customer => customer.ToResponse());
    }
}
