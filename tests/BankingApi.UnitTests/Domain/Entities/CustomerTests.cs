using BankingApi.Domain.Entities;
using BankingApi.Domain.Exceptions;
using FluentAssertions;

namespace BankingApi.UnitTests.Domain.Entities;

public class CustomerTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidValues_TrimsAndSetsProperties()
    {
        var customer = Customer.Create(" Jane Doe ", " jane@example.com ", " 199012345678 ", CreatedAt);

        customer.Id.Should().NotBeEmpty();
        customer.FullName.Should().Be("Jane Doe");
        customer.Email.Should().Be("jane@example.com");
        customer.NationalId.Should().Be("199012345678");
        customer.CreatedAt.Should().Be(CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_WithMissingFullName_ThrowsDomainValidationException(string? fullName)
    {
        var act = () => Customer.Create(fullName!, "jane@example.com", "123", CreatedAt);

        act.Should().Throw<DomainValidationException>().WithMessage("*FullName*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("jane@")]
    [InlineData("Jane <jane@example.com>")]
    public void Create_WithInvalidEmail_ThrowsDomainValidationException(string email)
    {
        var act = () => Customer.Create("Jane Doe", email, "123", CreatedAt);

        act.Should().Throw<DomainValidationException>().WithMessage("*Email*");
    }

    [Fact]
    public void Create_WithMissingNationalId_ThrowsDomainValidationException()
    {
        var act = () => Customer.Create("Jane Doe", "jane@example.com", " ", CreatedAt);

        act.Should().Throw<DomainValidationException>().WithMessage("*NationalId*");
    }

    [Fact]
    public void Create_WithTooLongFullName_ThrowsDomainValidationException()
    {
        var fullName = new string('a', Customer.FullNameMaxLength + 1);

        var act = () => Customer.Create(fullName, "jane@example.com", "123", CreatedAt);

        act.Should().Throw<DomainValidationException>().WithMessage("*FullName*");
    }
}
