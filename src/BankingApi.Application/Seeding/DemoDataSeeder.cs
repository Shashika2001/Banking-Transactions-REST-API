using BankingApi.Application.Abstractions;
using BankingApi.Application.Abstractions.Persistence;
using BankingApi.Application.Auth;
using BankingApi.Domain.Entities;
using Microsoft.Extensions.Options;

namespace BankingApi.Application.Seeding;

/// <summary>
/// Default implementation of <see cref="IDemoDataSeeder"/>. Creates two customers with logins, four accounts with
/// opening deposits, and one transfer between the customers. All money movements go through the domain, so balances
/// and transaction history are consistent.
/// </summary>
public sealed class DemoDataSeeder : IDemoDataSeeder
{
    /// <summary>The first demo customer's sign-in email.</summary>
    public const string JaneEmail = "jane.doe@example.com";

    /// <summary>The second demo customer's sign-in email.</summary>
    public const string JohnEmail = "john.smith@example.com";

    private const string OpeningDepositReference = "Opening deposit";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccountNumberGenerator _accountNumberGenerator;
    private readonly TimeProvider _timeProvider;
    private readonly DemoDataOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DemoDataSeeder"/> class.
    /// </summary>
    public DemoDataSeeder(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IAccountNumberGenerator accountNumberGenerator,
        TimeProvider timeProvider,
        IOptions<DemoDataOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _accountNumberGenerator = accountNumberGenerator;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<bool> SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrEmpty(_options.UserPassword))
        {
            return false;
        }

        if (_options.UserPassword.Length is < PasswordRules.MinimumLength or > PasswordRules.MaximumLength)
        {
            throw new InvalidOperationException(
                $"DemoData:UserPassword must be {PasswordRules.MinimumLength}–{PasswordRules.MaximumLength} characters.");
        }

        if (await _unitOfWork.Users.EmailExistsAsync(JaneEmail, cancellationToken))
        {
            return false;
        }

        var now = _timeProvider.GetUtcNow();
        var openedAt = now.AddDays(-7);
        var passwordHash = _passwordHasher.Hash(_options.UserPassword);

        var jane = await AddCustomerAsync("Jane Doe", JaneEmail, "199012345678", passwordHash, openedAt, cancellationToken);
        var john = await AddCustomerAsync("John Smith", JohnEmail, "198507654321", passwordHash, openedAt, cancellationToken);

        var janeChecking = await OpenFundedAccountAsync(jane.Id, "USD", 2_500.00m, openedAt, cancellationToken);
        await OpenFundedAccountAsync(jane.Id, "USD", 10_000.00m, openedAt, cancellationToken);
        var johnChecking = await OpenFundedAccountAsync(john.Id, "USD", 750.00m, openedAt, cancellationToken);
        await OpenFundedAccountAsync(john.Id, "EUR", 1_200.00m, openedAt, cancellationToken);

        var transfer = janeChecking.TransferTo(johnChecking, 150.00m, now.AddDays(-1), "Dinner split", "demo-seed-transfer-0001");
        await _unitOfWork.Transactions.AddAsync(transfer.Outgoing, cancellationToken);
        await _unitOfWork.Transactions.AddAsync(transfer.Incoming, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<Customer> AddCustomerAsync(
        string fullName,
        string email,
        string nationalId,
        string passwordHash,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        var customer = Customer.Create(fullName, email, nationalId, createdAt);
        await _unitOfWork.Customers.AddAsync(customer, cancellationToken);
        await _unitOfWork.Users.AddAsync(User.CreateCustomerUser(email, passwordHash, customer.Id, createdAt), cancellationToken);
        return customer;
    }

    private async Task<Account> OpenFundedAccountAsync(
        Guid customerId,
        string currency,
        decimal openingBalance,
        DateTimeOffset openedAt,
        CancellationToken cancellationToken)
    {
        var account = Account.Open(_accountNumberGenerator.Generate(), customerId, currency, openedAt);
        var deposit = account.Deposit(openingBalance, openedAt, OpeningDepositReference);

        await _unitOfWork.Accounts.AddAsync(account, cancellationToken);
        await _unitOfWork.Transactions.AddAsync(deposit, cancellationToken);
        return account;
    }
}
