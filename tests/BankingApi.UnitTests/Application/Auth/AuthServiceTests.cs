using BankingApi.Application.Abstractions;
using BankingApi.Application.Auditing;
using BankingApi.Application.Auth;
using BankingApi.Application.Common.Exceptions;
using BankingApi.Domain.Entities;
using BankingApi.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using Moq;

namespace BankingApi.UnitTests.Application.Auth;

public class AuthServiceTests
{
    private const string Password = "correct horse battery staple";
    private static readonly RegisterRequest ValidRegistration = new("Jane Doe", " jane@example.com ", "199012345678", Password);

    private readonly ApplicationTestContext _context = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly AccessToken _token = new("signed.jwt.token", ApplicationTestContext.Now.AddHours(1));

    public AuthServiceTests()
    {
        _passwordHasher.Setup(hasher => hasher.Hash(It.IsAny<string>())).Returns((string password) => $"hashed:{password}");
        _tokenService.Setup(service => service.CreateToken(It.IsAny<User>())).Returns(_token);
    }

    private AuthService CreateService() =>
        new(
            _context.UnitOfWork.Object,
            _passwordHasher.Object,
            _tokenService.Object,
            _context.AuditTrail.Object,
            _context.TimeProvider.Object,
            new RegisterRequestValidator(),
            new LoginRequestValidator());

    private User ArrangeExistingUser(PasswordVerificationResult verification)
    {
        var user = User.CreateCustomerUser("jane@example.com", "stored-hash", Guid.NewGuid(), ApplicationTestContext.Now);
        _context.Users
            .Setup(repository => repository.GetByEmailAsync("jane@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(hasher => hasher.Verify("stored-hash", Password)).Returns(verification);
        return user;
    }

    // ---- RegisterAsync ----------------------------------------------------------------------

    [Fact]
    public async Task RegisterAsync_WithValidRequest_CreatesLinkedCustomerAndCustomerRoleUser()
    {
        Customer? customer = null;
        User? user = null;
        _context.Customers
            .Setup(repository => repository.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Callback<Customer, CancellationToken>((added, _) => customer = added)
            .Returns(Task.CompletedTask);
        _context.Users
            .Setup(repository => repository.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((added, _) => user = added)
            .Returns(Task.CompletedTask);

        await CreateService().RegisterAsync(ValidRegistration);

        customer.Should().NotBeNull();
        user.Should().NotBeNull();
        user!.Role.Should().Be(UserRole.Customer);
        user.CustomerId.Should().Be(customer!.Id);
        user.Email.Should().Be("jane@example.com");
        user.PasswordHash.Should().Be($"hashed:{Password}").And.NotBe(Password);
        _context.VerifySavedOnce();
    }

    [Fact]
    public async Task RegisterAsync_WithValidRequest_ReturnsTokenAndMaskedCustomer()
    {
        var response = await CreateService().RegisterAsync(ValidRegistration);

        response.Auth.AccessToken.Should().Be(_token.Token);
        response.Auth.TokenType.Should().Be("Bearer");
        response.Auth.ExpiresAt.Should().Be(_token.ExpiresAt);
        response.Auth.Role.Should().Be("Customer");
        response.Auth.CustomerId.Should().Be(response.Customer.Id);
        response.Customer.MaskedNationalId.Should().Be("********5678");
    }

    [Fact]
    public async Task RegisterAsync_WithValidRequest_RecordsAuditEntry()
    {
        var response = await CreateService().RegisterAsync(ValidRegistration);

        _context.AuditTrail.Verify(
            audit => audit.RecordAsync(AuditActions.UserRegistered, nameof(User), response.Auth.UserId, It.IsNotNull<object>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailBelongsToUser_ThrowsConflictException()
    {
        _context.Users
            .Setup(repository => repository.EmailExistsAsync("jane@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => CreateService().RegisterAsync(ValidRegistration);

        await act.Should().ThrowAsync<ConflictException>();
        _context.VerifyNothingSaved();
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailBelongsToCustomer_ThrowsConflictException()
    {
        _context.Customers
            .Setup(repository => repository.EmailExistsAsync("jane@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => CreateService().RegisterAsync(ValidRegistration);

        await act.Should().ThrowAsync<ConflictException>();
        _context.VerifyNothingSaved();
    }

    [Fact]
    public async Task RegisterAsync_WhenNationalIdExists_ThrowsConflictException()
    {
        _context.Customers
            .Setup(repository => repository.NationalIdExistsAsync("199012345678", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => CreateService().RegisterAsync(ValidRegistration);

        await act.Should().ThrowAsync<ConflictException>();
        _context.VerifyNothingSaved();
    }

    [Theory]
    [InlineData("short")]
    [InlineData("")]
    public async Task RegisterAsync_WithWeakPassword_ThrowsValidationException(string password)
    {
        var act = () => CreateService().RegisterAsync(ValidRegistration with { Password = password });

        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Errors.Should().Contain(error => error.PropertyName == nameof(RegisterRequest.Password));
        _passwordHasher.Verify(hasher => hasher.Hash(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WithTooLongPassword_ThrowsValidationException()
    {
        var act = () => CreateService().RegisterAsync(ValidRegistration with { Password = new string('p', PasswordRules.MaximumLength + 1) });

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidCustomerDetails_ThrowsValidationException()
    {
        var act = () => CreateService().RegisterAsync(ValidRegistration with { Email = "not-an-email", NationalId = "" });

        var errors = (await act.Should().ThrowAsync<ValidationException>()).Which.Errors;
        errors.Should().Contain(error => error.PropertyName == nameof(RegisterRequest.Email));
        errors.Should().Contain(error => error.PropertyName == nameof(RegisterRequest.NationalId));
    }

    // ---- LoginAsync -------------------------------------------------------------------------

    [Fact]
    public async Task LoginAsync_WithCorrectPassword_ReturnsToken()
    {
        var user = ArrangeExistingUser(PasswordVerificationResult.Success);

        var response = await CreateService().LoginAsync(new LoginRequest(" jane@example.com ", Password));

        response.AccessToken.Should().Be(_token.Token);
        response.UserId.Should().Be(user.Id);
        response.Role.Should().Be("Customer");
        response.CustomerId.Should().Be(user.CustomerId);
        _context.VerifyNothingSaved();
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsInvalidCredentialsException()
    {
        ArrangeExistingUser(PasswordVerificationResult.Failed);

        var act = () => CreateService().LoginAsync(new LoginRequest("jane@example.com", Password));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        _tokenService.Verify(service => service.CreateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ThrowsSameExceptionAfterDummyVerification()
    {
        var act = () => CreateService().LoginAsync(new LoginRequest("nobody@example.com", Password));

        var exception = (await act.Should().ThrowAsync<InvalidCredentialsException>()).Which;
        exception.Message.Should().Be(new InvalidCredentialsException().Message);
        _passwordHasher.Verify(hasher => hasher.Verify(null, Password), Times.Once);
        _tokenService.Verify(service => service.CreateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenHashIsOutdated_RehashesAndSaves()
    {
        var user = ArrangeExistingUser(PasswordVerificationResult.SuccessRehashNeeded);

        await CreateService().LoginAsync(new LoginRequest("jane@example.com", Password));

        user.PasswordHash.Should().Be($"hashed:{Password}");
        _context.VerifySavedOnce();
    }

    [Theory]
    [InlineData("", Password)]
    [InlineData("jane@example.com", "")]
    public async Task LoginAsync_WithMissingFields_ThrowsValidationException(string email, string password)
    {
        var act = () => CreateService().LoginAsync(new LoginRequest(email, password));

        await act.Should().ThrowAsync<ValidationException>();
        _context.Users.Verify(
            repository => repository.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
