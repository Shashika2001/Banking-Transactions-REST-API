using BankingApi.Application.Abstractions;
using BankingApi.Application.Common.Exceptions;
using BankingApi.Application.Common.Security;
using BankingApi.Domain.Entities;
using FluentAssertions;
using Moq;

namespace BankingApi.UnitTests.Application.Security;

public class AccessPolicyTests
{
    private static readonly Guid OwnCustomerId = Guid.NewGuid();
    private static readonly Guid OtherCustomerId = Guid.NewGuid();

    private readonly Mock<ICurrentUser> _currentUser = new();

    private AccessPolicy CreatePolicy() => new(_currentUser.Object);

    private void SetUser(bool isAuthenticated, bool isAdmin, Guid? customerId)
    {
        _currentUser.SetupGet(user => user.IsAuthenticated).Returns(isAuthenticated);
        _currentUser.SetupGet(user => user.IsAdmin).Returns(isAdmin);
        _currentUser.SetupGet(user => user.CustomerId).Returns(customerId);
    }

    private static Account AccountOwnedBy(Guid customerId) =>
        Account.Open("ACC-1", customerId, "USD", DateTimeOffset.UnixEpoch);

    // ---- Customers --------------------------------------------------------------------------

    [Fact]
    public void EnsureCanAccessAccount_CustomerOwnAccount_Allows()
    {
        SetUser(isAuthenticated: true, isAdmin: false, OwnCustomerId);

        var act = () => CreatePolicy().EnsureCanAccessAccount(AccountOwnedBy(OwnCustomerId));

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanAccessAccount_CustomerOtherCustomersAccount_ThrowsForbiddenAccessException()
    {
        SetUser(isAuthenticated: true, isAdmin: false, OwnCustomerId);

        var act = () => CreatePolicy().EnsureCanAccessAccount(AccountOwnedBy(OtherCustomerId));

        act.Should().Throw<ForbiddenAccessException>().WithMessage("*account*");
    }

    [Fact]
    public void EnsureCanAccessCustomer_CustomerThemselves_Allows()
    {
        SetUser(isAuthenticated: true, isAdmin: false, OwnCustomerId);

        var act = () => CreatePolicy().EnsureCanAccessCustomer(OwnCustomerId);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanAccessCustomer_CustomerOtherCustomer_ThrowsForbiddenAccessException()
    {
        SetUser(isAuthenticated: true, isAdmin: false, OwnCustomerId);

        var act = () => CreatePolicy().EnsureCanAccessCustomer(OtherCustomerId);

        act.Should().Throw<ForbiddenAccessException>();
    }

    [Fact]
    public void EnsureCanAccessAccount_CustomerUserWithoutLinkedCustomer_ThrowsForbiddenAccessException()
    {
        SetUser(isAuthenticated: true, isAdmin: false, customerId: null);

        var act = () => CreatePolicy().EnsureCanAccessAccount(AccountOwnedBy(OtherCustomerId));

        act.Should().Throw<ForbiddenAccessException>();
    }

    [Fact]
    public void EnsureAdmin_Customer_ThrowsForbiddenAccessException()
    {
        SetUser(isAuthenticated: true, isAdmin: false, OwnCustomerId);

        var act = () => CreatePolicy().EnsureAdmin();

        act.Should().Throw<ForbiddenAccessException>();
    }

    // ---- Admins -----------------------------------------------------------------------------

    [Fact]
    public void EnsureCanAccessAccount_Admin_AllowsAnyAccount()
    {
        SetUser(isAuthenticated: true, isAdmin: true, customerId: null);

        var act = () => CreatePolicy().EnsureCanAccessAccount(AccountOwnedBy(OtherCustomerId));

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanAccessCustomer_Admin_AllowsAnyCustomer()
    {
        SetUser(isAuthenticated: true, isAdmin: true, customerId: null);

        var act = () => CreatePolicy().EnsureCanAccessCustomer(OtherCustomerId);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureAdmin_Admin_Allows()
    {
        SetUser(isAuthenticated: true, isAdmin: true, customerId: null);

        var act = () => CreatePolicy().EnsureAdmin();

        act.Should().NotThrow();
    }

    // ---- Unauthenticated --------------------------------------------------------------------

    [Fact]
    public void AllChecks_UnauthenticatedCaller_ThrowForbiddenAccessException()
    {
        // Even if stale claims suggested otherwise, an unauthenticated caller gets nothing.
        SetUser(isAuthenticated: false, isAdmin: true, OwnCustomerId);
        var policy = CreatePolicy();

        policy.Invoking(p => p.EnsureAdmin()).Should().Throw<ForbiddenAccessException>();
        policy.Invoking(p => p.EnsureCanAccessCustomer(OwnCustomerId)).Should().Throw<ForbiddenAccessException>();
        policy.Invoking(p => p.EnsureCanAccessAccount(AccountOwnedBy(OwnCustomerId))).Should().Throw<ForbiddenAccessException>();
    }

    [Fact]
    public void EnsureCanAccessAccount_NullAccount_ThrowsArgumentNullException()
    {
        SetUser(isAuthenticated: true, isAdmin: true, customerId: null);

        var act = () => CreatePolicy().EnsureCanAccessAccount(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
