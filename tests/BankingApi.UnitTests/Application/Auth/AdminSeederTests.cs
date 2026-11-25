using BankingApi.Application.Abstractions;
using BankingApi.Application.Auth;
using BankingApi.Domain.Entities;
using BankingApi.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace BankingApi.UnitTests.Application.Auth;

public class AdminSeederTests
{
    private const string AdminEmail = "admin@bank.example";
    private const string AdminPassword = "a-long-admin-password";

    private readonly ApplicationTestContext _context = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();

    public AdminSeederTests()
    {
        _passwordHasher.Setup(hasher => hasher.Hash(It.IsAny<string>())).Returns("hashed");
    }

    private AdminSeeder CreateSeeder(string? email, string? password) =>
        new(
            _context.UnitOfWork.Object,
            _passwordHasher.Object,
            _context.TimeProvider.Object,
            Options.Create(new BootstrapAdminOptions { Email = email, Password = password }));

    [Theory]
    [InlineData(null, null)]
    [InlineData(AdminEmail, null)]
    [InlineData(null, AdminPassword)]
    [InlineData(" ", AdminPassword)]
    public async Task SeedAsync_WhenNotConfigured_DoesNothing(string? email, string? password)
    {
        var created = await CreateSeeder(email, password).SeedAsync();

        created.Should().BeFalse();
        _context.Users.VerifyNoOtherCalls();
        _context.VerifyNothingSaved();
    }

    [Fact]
    public async Task SeedAsync_WhenAdminDoesNotExist_CreatesAdminWithHashedPassword()
    {
        User? added = null;
        _context.Users
            .Setup(repository => repository.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => added = user)
            .Returns(Task.CompletedTask);

        var created = await CreateSeeder(AdminEmail, AdminPassword).SeedAsync();

        created.Should().BeTrue();
        added!.Role.Should().Be(UserRole.Admin);
        added.Email.Should().Be(AdminEmail);
        added.CustomerId.Should().BeNull();
        added.PasswordHash.Should().Be("hashed");
        _context.VerifySavedOnce();
    }

    [Fact]
    public async Task SeedAsync_WhenUserAlreadyExists_DoesNotCreateAnother()
    {
        _context.Users
            .Setup(repository => repository.EmailExistsAsync(AdminEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var created = await CreateSeeder(AdminEmail, AdminPassword).SeedAsync();

        created.Should().BeFalse();
        _context.VerifyNothingSaved();
    }

    [Fact]
    public async Task SeedAsync_WithTooShortPassword_ThrowsInvalidOperationException()
    {
        var act = () => CreateSeeder(AdminEmail, "short").SeedAsync();

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*BootstrapAdmin:Password*");
        _context.VerifyNothingSaved();
    }
}
