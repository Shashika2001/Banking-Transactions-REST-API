using BankingApi.Application.AuditLogs;
using BankingApi.Application.Common.Exceptions;
using BankingApi.Application.Common.Models;
using BankingApi.Domain.Entities;
using FluentAssertions;
using FluentValidation;
using Moq;

namespace BankingApi.UnitTests.Application.AuditLogs;

public class AuditLogServiceTests
{
    private readonly ApplicationTestContext _context = new();

    private AuditLogService CreateService() =>
        new(_context.UnitOfWork.Object, _context.AccessPolicy, new AuditLogQueryValidator());

    [Fact]
    public async Task SearchAsync_AsAdmin_ReturnsMappedPage()
    {
        var entry = AuditLog.Create("user-1", "Deposit", "Account", "abc", """{"amount":10}""", ApplicationTestContext.Now);
        var query = new AuditLogQuery { EntityName = "Account", PageNumber = 1, PageSize = 10 };
        _context.AuditLogs
            .Setup(repository => repository.SearchAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AuditLog>([entry], 1, 10, 1));

        var page = await CreateService().SearchAsync(query);

        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new AuditLogResponse(
            entry.Id, "user-1", "Deposit", "Account", "abc", """{"amount":10}""", ApplicationTestContext.Now));
    }

    [Fact]
    public async Task SearchAsync_AsCustomer_ThrowsForbiddenAccessException()
    {
        _context.ActAsCustomer(Guid.NewGuid());

        var act = () => CreateService().SearchAsync(new AuditLogQuery());

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        _context.AuditLogs.Verify(
            repository => repository.SearchAsync(It.IsAny<AuditLogQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchAsync_WithToBeforeFrom_ThrowsValidationException()
    {
        var query = new AuditLogQuery { From = ApplicationTestContext.Now, To = ApplicationTestContext.Now.AddDays(-1) };

        var act = () => CreateService().SearchAsync(query);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task SearchAsync_WithInvalidPageSize_ThrowsValidationException()
    {
        var act = () => CreateService().SearchAsync(new AuditLogQuery { PageSize = 1_000 });

        await act.Should().ThrowAsync<ValidationException>();
    }
}
