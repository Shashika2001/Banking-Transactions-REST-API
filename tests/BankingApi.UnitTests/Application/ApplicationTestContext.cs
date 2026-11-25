using BankingApi.Application.Abstractions;
using BankingApi.Application.Abstractions.Persistence;
using BankingApi.Application.Auditing;
using BankingApi.Application.Common.Security;
using BankingApi.Domain.Entities;
using Moq;

namespace BankingApi.UnitTests.Application;

/// <summary>
/// Shared mocks for Application service tests: a unit of work wired to repository mocks, an audit trail mock,
/// a clock fixed at <see cref="Now"/>, and a current user (an admin unless <see cref="ActAsCustomer"/> is called)
/// checked by the real <see cref="Application.Common.Security.AccessPolicy"/>.
/// </summary>
internal sealed class ApplicationTestContext
{
    public static readonly DateTimeOffset Now = new(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);

    public ApplicationTestContext()
    {
        UnitOfWork.SetupGet(unitOfWork => unitOfWork.Customers).Returns(Customers.Object);
        UnitOfWork.SetupGet(unitOfWork => unitOfWork.Accounts).Returns(Accounts.Object);
        UnitOfWork.SetupGet(unitOfWork => unitOfWork.Transactions).Returns(Transactions.Object);
        UnitOfWork.SetupGet(unitOfWork => unitOfWork.AuditLogs).Returns(AuditLogs.Object);
        UnitOfWork.SetupGet(unitOfWork => unitOfWork.Users).Returns(Users.Object);
        TimeProvider.Setup(clock => clock.GetUtcNow()).Returns(Now);
        AccessPolicy = new AccessPolicy(CurrentUser.Object);
        ActAsAdmin();
    }

    public Mock<ICurrentUser> CurrentUser { get; } = new();

    public IAccessPolicy AccessPolicy { get; }

    public void ActAsAdmin()
    {
        CurrentUser.SetupGet(user => user.IsAuthenticated).Returns(true);
        CurrentUser.SetupGet(user => user.IsAdmin).Returns(true);
        CurrentUser.SetupGet(user => user.CustomerId).Returns((Guid?)null);
        CurrentUser.SetupGet(user => user.UserId).Returns("admin-user");
    }

    public void ActAsCustomer(Guid customerId)
    {
        CurrentUser.SetupGet(user => user.IsAuthenticated).Returns(true);
        CurrentUser.SetupGet(user => user.IsAdmin).Returns(false);
        CurrentUser.SetupGet(user => user.CustomerId).Returns(customerId);
        CurrentUser.SetupGet(user => user.UserId).Returns($"user-of-{customerId}");
    }

    public Mock<IUnitOfWork> UnitOfWork { get; } = new();

    public Mock<ICustomerRepository> Customers { get; } = new();

    public Mock<IAccountRepository> Accounts { get; } = new();

    public Mock<ITransactionRepository> Transactions { get; } = new();

    public Mock<IAuditLogRepository> AuditLogs { get; } = new();

    public Mock<IUserRepository> Users { get; } = new();

    public Mock<IAuditTrail> AuditTrail { get; } = new();

    public Mock<TimeProvider> TimeProvider { get; } = new();

    /// <summary>Makes <see cref="IUnitOfWork.ExecuteInTransactionAsync{TResult}"/> run the operation directly.</summary>
    public void RunTransactionsInline<TResult>() =>
        UnitOfWork
            .Setup(unitOfWork => unitOfWork.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<TResult>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<TResult>> operation, CancellationToken token) => operation(token));

    /// <summary>Registers an account so <see cref="IRepository{TEntity}.GetByIdAsync"/> returns it.</summary>
    public void AddAccount(Account account)
    {
        Accounts.Setup(repository => repository.GetByIdAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        Accounts.Setup(repository => repository.ExistsAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    public void VerifyNothingSaved() =>
        UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

    public void VerifySavedOnce() =>
        UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

    /// <summary>Creates an active account, optionally funded with an opening deposit.</summary>
    public static Account CreateAccount(decimal balance = 0m, string currency = "USD")
    {
        var account = Account.Open($"ACC-{Guid.NewGuid():N}"[..20], Guid.NewGuid(), currency, Now.AddDays(-30));
        if (balance > 0m)
        {
            account.Deposit(balance, Now.AddDays(-30));
        }

        return account;
    }
}
