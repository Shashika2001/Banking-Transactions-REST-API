using BankingApi.Application.Abstractions;
using BankingApi.Application.Abstractions.Persistence;
using BankingApi.Domain.Entities;
using Microsoft.Extensions.Options;

namespace BankingApi.Application.Auth;

/// <summary>
/// Default implementation of <see cref="IAdminSeeder"/>.
/// </summary>
public sealed class AdminSeeder : IAdminSeeder
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly TimeProvider _timeProvider;
    private readonly BootstrapAdminOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminSeeder"/> class.
    /// </summary>
    public AdminSeeder(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        TimeProvider timeProvider,
        IOptions<BootstrapAdminOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<bool> SeedAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Email) || string.IsNullOrEmpty(_options.Password))
        {
            return false;
        }

        if (_options.Password.Length is < PasswordRules.MinimumLength or > PasswordRules.MaximumLength)
        {
            throw new InvalidOperationException(
                $"BootstrapAdmin:Password must be {PasswordRules.MinimumLength}–{PasswordRules.MaximumLength} characters.");
        }

        var email = _options.Email.Trim();
        if (await _unitOfWork.Users.EmailExistsAsync(email, cancellationToken))
        {
            return false;
        }

        var admin = User.CreateAdmin(email, _passwordHasher.Hash(_options.Password), _timeProvider.GetUtcNow());
        await _unitOfWork.Users.AddAsync(admin, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
