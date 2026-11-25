using BankingApi.Domain.Entities;
using BankingApi.Infrastructure.Authentication;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace BankingApi.UnitTests.Infrastructure;

public class JwtTokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);

    private readonly JwtOptions _options = new()
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        SigningKey = "unit-test-signing-key-that-is-long-enough-0123456789",
        AccessTokenLifetimeMinutes = 30,
    };

    private JwtTokenService CreateService()
    {
        var clock = new Mock<TimeProvider>();
        clock.Setup(provider => provider.GetUtcNow()).Returns(Now);
        return new JwtTokenService(Options.Create(_options), clock.Object);
    }

    private Task<TokenValidationResult> ValidateAsync(string token, string signingKey) =>
        new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = new JwtOptions { SigningKey = signingKey }.CreateSigningKey(),
            ValidateLifetime = false, // The fixed test clock is not the real clock.
        });

    [Fact]
    public void CreateToken_ForCustomerUser_ContainsIdentityRoleAndCustomerClaims()
    {
        var user = User.CreateCustomerUser("jane@example.com", "hash", Guid.NewGuid(), Now);

        var token = new JsonWebToken(CreateService().CreateToken(user).Token);

        token.GetClaim(BankingClaimTypes.Subject).Value.Should().Be(user.Id.ToString());
        token.GetClaim(BankingClaimTypes.Email).Value.Should().Be("jane@example.com");
        token.GetClaim(BankingClaimTypes.Role).Value.Should().Be("Customer");
        token.GetClaim(BankingClaimTypes.CustomerId).Value.Should().Be(user.CustomerId.ToString());
        token.Issuer.Should().Be(_options.Issuer);
        token.Audiences.Should().ContainSingle().Which.Should().Be(_options.Audience);
    }

    [Fact]
    public void CreateToken_ForAdmin_HasAdminRoleAndNoCustomerClaim()
    {
        var admin = User.CreateAdmin("admin@example.com", "hash", Now);

        var token = new JsonWebToken(CreateService().CreateToken(admin).Token);

        token.GetClaim(BankingClaimTypes.Role).Value.Should().Be("Admin");
        token.TryGetClaim(BankingClaimTypes.CustomerId, out _).Should().BeFalse();
    }

    [Fact]
    public void CreateToken_Always_ExpiresAfterConfiguredLifetime()
    {
        var accessToken = CreateService().CreateToken(User.CreateAdmin("admin@example.com", "hash", Now));

        accessToken.ExpiresAt.Should().Be(Now.AddMinutes(30));
        new JsonWebToken(accessToken.Token).ValidTo.Should().Be(Now.AddMinutes(30).UtcDateTime);
    }

    [Fact]
    public async Task CreateToken_ValidatedWithSameKey_IsValid()
    {
        var accessToken = CreateService().CreateToken(User.CreateAdmin("admin@example.com", "hash", Now));

        var result = await ValidateAsync(accessToken.Token, _options.SigningKey);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CreateToken_ValidatedWithDifferentKey_IsRejected()
    {
        var accessToken = CreateService().CreateToken(User.CreateAdmin("admin@example.com", "hash", Now));

        var result = await ValidateAsync(accessToken.Token, "a-completely-different-signing-key-0123456789");

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("", "aud", "unit-test-signing-key-that-is-long-enough-0123456789", 60)]
    [InlineData("iss", "", "unit-test-signing-key-that-is-long-enough-0123456789", 60)]
    [InlineData("iss", "aud", "too-short-key", 60)]
    [InlineData("iss", "aud", "unit-test-signing-key-that-is-long-enough-0123456789", 0)]
    public void JwtOptions_WithMissingOrWeakSettings_IsInvalid(string issuer, string audience, string signingKey, int lifetime)
    {
        var options = new JwtOptions { Issuer = issuer, Audience = audience, SigningKey = signingKey, AccessTokenLifetimeMinutes = lifetime };

        options.IsValid().Should().BeFalse();
    }
}
