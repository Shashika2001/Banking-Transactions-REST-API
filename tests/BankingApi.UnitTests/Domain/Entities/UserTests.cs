using BankingApi.Domain.Entities;
using BankingApi.Domain.Enums;
using BankingApi.Domain.Exceptions;
using FluentAssertions;

namespace BankingApi.UnitTests.Domain.Entities;

public class UserTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateCustomerUser_WithValidValues_LinksCustomerWithCustomerRole()
    {
        var customerId = Guid.NewGuid();

        var user = User.CreateCustomerUser(" jane@example.com ", "hash", customerId, CreatedAt);

        user.Email.Should().Be("jane@example.com");
        user.Role.Should().Be(UserRole.Customer);
        user.CustomerId.Should().Be(customerId);
        user.CreatedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public void CreateCustomerUser_WithoutCustomer_ThrowsDomainValidationException()
    {
        var act = () => User.CreateCustomerUser("jane@example.com", "hash", Guid.Empty, CreatedAt);

        act.Should().Throw<DomainValidationException>().WithMessage("*CustomerId*");
    }

    [Fact]
    public void CreateAdmin_WithValidValues_HasAdminRoleAndNoCustomer()
    {
        var user = User.CreateAdmin("admin@example.com", "hash", CreatedAt);

        user.Role.Should().Be(UserRole.Admin);
        user.CustomerId.Should().BeNull();
    }

    [Theory]
    [InlineData("", "hash")]
    [InlineData("admin@example.com", "")]
    public void CreateAdmin_WithMissingValues_ThrowsDomainValidationException(string email, string passwordHash)
    {
        var act = () => User.CreateAdmin(email, passwordHash, CreatedAt);

        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void ChangePasswordHash_WithNewHash_ReplacesHash()
    {
        var user = User.CreateAdmin("admin@example.com", "old-hash", CreatedAt);

        user.ChangePasswordHash("new-hash");

        user.PasswordHash.Should().Be("new-hash");
    }
}
