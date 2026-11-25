using BankingApi.Application.Abstractions;
using BankingApi.Application.Abstractions.Persistence;
using BankingApi.Application.Auditing;
using BankingApi.Application.Common.Exceptions;
using BankingApi.Application.Customers;
using BankingApi.Domain.Entities;
using FluentValidation;

namespace BankingApi.Application.Auth;

/// <summary>
/// Default implementation of <see cref="IAuthService"/>.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IAuditTrail _auditTrail;
    private readonly TimeProvider _timeProvider;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class.
    /// </summary>
    public AuthService(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IAuditTrail auditTrail,
        TimeProvider timeProvider,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _auditTrail = auditTrail;
        _timeProvider = timeProvider;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    /// <inheritdoc />
    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        await _registerValidator.ValidateAndThrowAsync(request, cancellationToken);

        var email = request.Email.Trim();
        var nationalId = request.NationalId.Trim();

        if (await _unitOfWork.Users.EmailExistsAsync(email, cancellationToken) ||
            await _unitOfWork.Customers.EmailExistsAsync(email, cancellationToken))
        {
            throw new ConflictException("An account with this email address already exists.");
        }

        if (await _unitOfWork.Customers.NationalIdExistsAsync(nationalId, cancellationToken))
        {
            throw new ConflictException("A customer with this national ID already exists.");
        }

        var now = _timeProvider.GetUtcNow();
        var customer = Customer.Create(request.FullName, email, nationalId, now);
        var user = User.CreateCustomerUser(email, _passwordHasher.Hash(request.Password), customer.Id, now);

        await _unitOfWork.Customers.AddAsync(customer, cancellationToken);
        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _auditTrail.RecordAsync(
            AuditActions.UserRegistered,
            nameof(User),
            user.Id,
            new { CustomerId = customer.Id },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterResponse(CreateAuthResponse(user), customer.ToResponse());
    }

    /// <inheritdoc />
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        await _loginValidator.ValidateAndThrowAsync(request, cancellationToken);

        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email.Trim(), cancellationToken);

        // Verify even when the user does not exist, so timing does not reveal registered emails.
        var result = _passwordHasher.Verify(user?.PasswordHash, request.Password);
        if (user is null || result == PasswordVerificationResult.Failed)
        {
            throw new InvalidCredentialsException();
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.ChangePasswordHash(_passwordHasher.Hash(request.Password));
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return CreateAuthResponse(user);
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var token = _tokenService.CreateToken(user);
        return new AuthResponse(
            token.Token,
            "Bearer",
            token.ExpiresAt,
            user.Id,
            user.Email,
            user.Role.ToString(),
            user.CustomerId);
    }
}
