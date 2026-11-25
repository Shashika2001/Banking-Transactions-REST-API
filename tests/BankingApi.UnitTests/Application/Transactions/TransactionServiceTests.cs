using BankingApi.Application.Common.Exceptions;
using BankingApi.Application.Common.Models;
using BankingApi.Application.Transactions;
using BankingApi.Domain.Entities;
using FluentAssertions;
using FluentValidation;
using Moq;

namespace BankingApi.UnitTests.Application.Transactions;

public class TransactionServiceTests
{
    private static readonly DateTimeOffset From = new(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset To = new(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly ApplicationTestContext _context = new();

    private TransactionService CreateService() =>
        new(_context.UnitOfWork.Object, new TransactionHistoryQueryValidator());

    [Fact]
    public async Task GetHistoryAsync_WithValidQuery_ReturnsMappedPage()
    {
        var account = ApplicationTestContext.CreateAccount();
        _context.AddAccount(account);
        var deposit = account.Deposit(100m, From.AddDays(1), "Salary");
        var withdrawal = account.Withdraw(25m, From.AddDays(2));
        _context.Transactions
            .Setup(repository => repository.GetHistoryAsync(account.Id, From, To, 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Transaction>([withdrawal, deposit], 2, 10, 12));

        var page = await CreateService().GetHistoryAsync(
            account.Id,
            new TransactionHistoryQuery { From = From, To = To, PageNumber = 2, PageSize = 10 });

        page.TotalCount.Should().Be(12);
        page.TotalPages.Should().Be(2);
        page.Items.Should().HaveCount(2);
        page.Items[0].Id.Should().Be(withdrawal.Id);
        page.Items[0].Type.Should().Be("Withdrawal");
        page.Items[0].BalanceAfter.Should().Be(75m);
        page.Items[1].Type.Should().Be("Deposit");
        page.Items[1].Reference.Should().Be("Salary");
    }

    [Fact]
    public async Task GetHistoryAsync_WithoutDateRange_PassesNullBounds()
    {
        var account = ApplicationTestContext.CreateAccount();
        _context.AddAccount(account);
        _context.Transactions
            .Setup(repository => repository.GetHistoryAsync(account.Id, null, null, 1, PageRequest.DefaultPageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Transaction>([], 1, PageRequest.DefaultPageSize, 0));

        var page = await CreateService().GetHistoryAsync(account.Id, new TransactionHistoryQuery());

        page.Items.Should().BeEmpty();
        page.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task GetHistoryAsync_MissingAccount_ThrowsNotFoundException()
    {
        var act = () => CreateService().GetHistoryAsync(Guid.NewGuid(), new TransactionHistoryQuery());

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Account*");
    }

    [Fact]
    public async Task GetHistoryAsync_WithToBeforeFrom_ThrowsValidationException()
    {
        var account = ApplicationTestContext.CreateAccount();
        _context.AddAccount(account);

        var act = () => CreateService().GetHistoryAsync(account.Id, new TransactionHistoryQuery { From = To, To = From });

        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Errors.Should().Contain(error => error.PropertyName == nameof(TransactionHistoryQuery.To));
    }

    [Fact]
    public async Task GetHistoryAsync_WithEqualFromAndTo_ThrowsValidationException()
    {
        var account = ApplicationTestContext.CreateAccount();
        _context.AddAccount(account);

        var act = () => CreateService().GetHistoryAsync(account.Id, new TransactionHistoryQuery { From = From, To = From });

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetHistoryAsync_WithInvalidPaging_ThrowsValidationException(int pageNumber, int pageSize)
    {
        var act = () => CreateService().GetHistoryAsync(
            Guid.NewGuid(),
            new TransactionHistoryQuery { PageNumber = pageNumber, PageSize = pageSize });

        await act.Should().ThrowAsync<ValidationException>();
    }
}
