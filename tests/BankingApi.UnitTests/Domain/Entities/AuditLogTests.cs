using BankingApi.Domain.Entities;
using BankingApi.Domain.Exceptions;
using FluentAssertions;

namespace BankingApi.UnitTests.Domain.Entities;

public class AuditLogTests
{
    private static readonly DateTimeOffset Timestamp = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidValues_SetsProperties()
    {
        var auditLog = AuditLog.Create("user-1", "Deposit", "Account", "abc", "Amount 10.00", Timestamp);

        auditLog.Id.Should().NotBeEmpty();
        auditLog.UserId.Should().Be("user-1");
        auditLog.Action.Should().Be("Deposit");
        auditLog.EntityName.Should().Be("Account");
        auditLog.EntityId.Should().Be("abc");
        auditLog.Details.Should().Be("Amount 10.00");
        auditLog.Timestamp.Should().Be(Timestamp);
    }

    [Fact]
    public void Create_WithoutDetails_LeavesDetailsNull()
    {
        var auditLog = AuditLog.Create("user-1", "Deposit", "Account", "abc", null, Timestamp);

        auditLog.Details.Should().BeNull();
    }

    [Theory]
    [InlineData("", "Deposit", "Account", "abc", "UserId")]
    [InlineData("user-1", " ", "Account", "abc", "Action")]
    [InlineData("user-1", "Deposit", "", "abc", "EntityName")]
    [InlineData("user-1", "Deposit", "Account", "", "EntityId")]
    public void Create_WithMissingRequiredValue_ThrowsDomainValidationException(
        string userId,
        string action,
        string entityName,
        string entityId,
        string expectedField)
    {
        var act = () => AuditLog.Create(userId, action, entityName, entityId, null, Timestamp);

        act.Should().Throw<DomainValidationException>().WithMessage($"*{expectedField}*");
    }
}
