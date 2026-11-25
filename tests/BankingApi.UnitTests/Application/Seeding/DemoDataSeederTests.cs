using BankingApi.Application.Abstractions;
using BankingApi.Application.Seeding;
using BankingApi.Domain.Entities;
using BankingApi.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace BankingApi.UnitTests.Application.Seeding;

public class DemoDataSeederTests
{
    private const string DemoPassword = "demo-password-123";

    private readonly ApplicationTestContext _context = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IAccountNumberGenerator> _accountNumberGenerator = new();
    private readonly List<Customer> _customers = [];
    private readonly List<User> _users = [];
    private readonly List<Account> _accounts = [];
    private readonly List<Transaction> _transactions = [];

    public DemoDataSeederTests()
    {
        _passwordHasher.Setup(hasher => hasher.Hash(DemoPassword)).Returns("hashed-demo-password");
        var nextNumber = 100_000_000_000L;
        _accountNumberGenerator.Setup(generator => generator.Generate()).Returns(() => (nextNumber++).ToString(System.Globalization.CultureInfo.InvariantCulture));

        _context.Customers.Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Callback<Customer, CancellationToken>((entity, _) => _customers.Add(entity)).Returns(Task.CompletedTask);
        _context.Users.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((entity, _) => _users.Add(entity)).Returns(Task.CompletedTask);
        _context.Accounts.Setup(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .Callback<Account, CancellationToken>((entity, _) => _accounts.Add(entity)).Returns(Task.CompletedTask);
        _context.Transactions.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((entity, _) => _transactions.Add(entity)).Returns(Task.CompletedTask);
    }

    private DemoDataSeeder CreateSeeder(bool enabled = true, string? password = DemoPassword) =>
        new(
            _context.UnitOfWork.Object,
            _passwordHasher.Object,
            _accountNumberGenerator.Object,
            _context.TimeProvider.Object,
            Options.Create(new DemoDataOptions { Enabled = enabled, UserPassword = password }));

    [Fact]
    public async Task SeedAsync_WhenEnabled_CreatesTwoCustomersWithCustomerLogins()
    {
        var created = await CreateSeeder().SeedAsync();

        created.Should().BeTrue();
        _customers.Select(customer => customer.Email)
            .Should().BeEquivalentTo([DemoDataSeeder.JaneEmail, DemoDataSeeder.JohnEmail]);
        _users.Should().HaveCount(2).And.OnlyContain(user => user.Role == UserRole.Customer);
        _users.Select(user => user.CustomerId).Should().BeEquivalentTo(_customers.Select(customer => (Guid?)customer.Id));
        _users.Should().OnlyContain(user => user.PasswordHash == "hashed-demo-password");
        _context.VerifySavedOnce();
    }

    [Fact]
    public async Task SeedAsync_WhenEnabled_CreatesFundedAccountsWithConsistentHistory()
    {
        await CreateSeeder().SeedAsync();

        _accounts.Should().HaveCount(4);
        _accounts.Select(account => account.Currency).Should().BeEquivalentTo(["USD", "USD", "USD", "EUR"]);
        _accounts.Select(account => account.Balance).Should().BeEquivalentTo([2_350.00m, 10_000.00m, 900.00m, 1_200.00m]);

        // Every account's balance equals the balance recorded on its latest transaction.
        foreach (var account in _accounts)
        {
            _transactions.Where(transaction => transaction.AccountId == account.Id)
                .OrderBy(transaction => transaction.CreatedAt)
                .Last().BalanceAfter.Should().Be(account.Balance);
        }
    }

    [Fact]
    public async Task SeedAsync_WhenEnabled_RecordsOneTransferBetweenCustomers()
    {
        await CreateSeeder().SeedAsync();

        var outgoing = _transactions.Should().ContainSingle(transaction => transaction.Type == TransactionType.TransferOut).Subject;
        var incoming = _transactions.Should().ContainSingle(transaction => transaction.Type == TransactionType.TransferIn).Subject;
        outgoing.TransferId.Should().NotBeNull().And.Be(incoming.TransferId);
        outgoing.Amount.Should().Be(150.00m);
        _transactions.Count(transaction => transaction.Type == TransactionType.Deposit).Should().Be(4);
    }

    [Fact]
    public async Task SeedAsync_WhenAlreadySeeded_DoesNothing()
    {
        _context.Users
            .Setup(repository => repository.EmailExistsAsync(DemoDataSeeder.JaneEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var created = await CreateSeeder().SeedAsync();

        created.Should().BeFalse();
        _customers.Should().BeEmpty();
        _context.VerifyNothingSaved();
    }

    [Theory]
    [InlineData(false, DemoPassword)]
    [InlineData(true, null)]
    [InlineData(true, "")]
    public async Task SeedAsync_WhenDisabledOrWithoutPassword_DoesNothing(bool enabled, string? password)
    {
        var created = await CreateSeeder(enabled, password).SeedAsync();

        created.Should().BeFalse();
        _context.Users.VerifyNoOtherCalls();
        _context.VerifyNothingSaved();
    }

    [Fact]
    public async Task SeedAsync_WithTooShortPassword_ThrowsInvalidOperationException()
    {
        var act = () => CreateSeeder(password: "short").SeedAsync();

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*DemoData:UserPassword*");
        _context.VerifyNothingSaved();
    }
}
