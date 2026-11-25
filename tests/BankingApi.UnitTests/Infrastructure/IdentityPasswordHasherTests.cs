using BankingApi.Application.Abstractions;
using BankingApi.Infrastructure.Authentication;
using FluentAssertions;

namespace BankingApi.UnitTests.Infrastructure;

public class IdentityPasswordHasherTests
{
    private const string Password = "correct horse battery staple";

    private readonly IdentityPasswordHasher _hasher = new();

    [Fact]
    public void Hash_Password_DoesNotContainPlainText()
    {
        var hash = _hasher.Hash(Password);

        hash.Should().NotBeNullOrEmpty().And.NotContain(Password);
    }

    [Fact]
    public void Hash_SamePasswordTwice_UsesDifferentSalts()
    {
        _hasher.Hash(Password).Should().NotBe(_hasher.Hash(Password));
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsSuccess()
    {
        var hash = _hasher.Hash(Password);

        _hasher.Verify(hash, Password).Should().Be(PasswordVerificationResult.Success);
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFailed()
    {
        var hash = _hasher.Hash(Password);

        _hasher.Verify(hash, "wrong password!").Should().Be(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void Verify_WithoutStoredHash_ReturnsFailed()
    {
        _hasher.Verify(null, Password).Should().Be(PasswordVerificationResult.Failed);
    }
}
