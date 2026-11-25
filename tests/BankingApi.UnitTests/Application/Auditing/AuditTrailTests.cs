using BankingApi.Application.Abstractions;
using BankingApi.Application.Auditing;
using BankingApi.Domain.Entities;
using FluentAssertions;
using Moq;

namespace BankingApi.UnitTests.Application.Auditing;

public class AuditTrailTests
{
    private readonly ApplicationTestContext _context = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private AuditLog? _recorded;

    public AuditTrailTests()
    {
        _currentUser.SetupGet(user => user.UserId).Returns("teller-42");
        _context.AuditLogs
            .Setup(repository => repository.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((auditLog, _) => _recorded = auditLog)
            .Returns(Task.CompletedTask);
    }

    private AuditTrail CreateAuditTrail() =>
        new(_context.AuditLogs.Object, _currentUser.Object, _context.TimeProvider.Object);

    [Fact]
    public async Task RecordAsync_WithDetails_AddsEntryForCurrentUserWithJsonDetails()
    {
        var entityId = Guid.NewGuid();

        await CreateAuditTrail().RecordAsync(AuditActions.Deposit, "Account", entityId, new { Amount = 10.5m, Currency = "USD" });

        _recorded.Should().NotBeNull();
        _recorded!.UserId.Should().Be("teller-42");
        _recorded.Action.Should().Be(AuditActions.Deposit);
        _recorded.EntityName.Should().Be("Account");
        _recorded.EntityId.Should().Be(entityId.ToString());
        _recorded.Details.Should().Be("""{"amount":10.5,"currency":"USD"}""");
        _recorded.Timestamp.Should().Be(ApplicationTestContext.Now);
    }

    [Fact]
    public async Task RecordAsync_WithoutDetails_LeavesDetailsNull()
    {
        await CreateAuditTrail().RecordAsync(AuditActions.CustomerCreated, "Customer", Guid.NewGuid());

        _recorded!.Details.Should().BeNull();
    }
}
