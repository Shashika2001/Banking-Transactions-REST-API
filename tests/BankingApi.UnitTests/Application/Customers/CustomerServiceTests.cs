using BankingApi.Application.Auditing;
using BankingApi.Application.Common.Exceptions;
using BankingApi.Application.Common.Models;
using BankingApi.Application.Common.Validation;
using BankingApi.Application.Customers;
using BankingApi.Domain.Entities;
using FluentAssertions;
using FluentValidation;
using Moq;

namespace BankingApi.UnitTests.Application.Customers;

public class CustomerServiceTests
{
    private static readonly CreateCustomerRequest ValidRequest = new("Jane Doe", " jane@example.com ", "199012345678");

    private readonly ApplicationTestContext _context = new();

    private CustomerService CreateService() =>
        new(
            _context.UnitOfWork.Object,
            _context.AuditTrail.Object,
            _context.TimeProvider.Object,
            new CreateCustomerRequestValidator(),
            new PageRequestValidator());

    [Fact]
    public async Task CreateAsync_WithValidRequest_SavesCustomerAndReturnsResponse()
    {
        Customer? added = null;
        _context.Customers
            .Setup(repository => repository.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Callback<Customer, CancellationToken>((customer, _) => added = customer)
            .Returns(Task.CompletedTask);

        var response = await CreateService().CreateAsync(ValidRequest);

        added.Should().NotBeNull();
        response.Id.Should().Be(added!.Id);
        response.FullName.Should().Be("Jane Doe");
        response.Email.Should().Be("jane@example.com");
        response.CreatedAt.Should().Be(ApplicationTestContext.Now);
        _context.VerifySavedOnce();
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_MasksNationalIdInResponse()
    {
        var response = await CreateService().CreateAsync(ValidRequest);

        response.MaskedNationalId.Should().Be("********5678");
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_RecordsAuditEntry()
    {
        var response = await CreateService().CreateAsync(ValidRequest);

        _context.AuditTrail.Verify(
            audit => audit.RecordAsync(AuditActions.CustomerCreated, nameof(Customer), response.Id, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("", "jane@example.com", "199012345678", "FullName")]
    [InlineData("Jane Doe", "not-an-email", "199012345678", "Email")]
    [InlineData("Jane Doe", "jane@example.com", "", "NationalId")]
    [InlineData("Jane Doe", "jane@example.com", "1990 1234", "NationalId")]
    public async Task CreateAsync_WithInvalidRequest_ThrowsValidationExceptionAndSavesNothing(
        string fullName,
        string email,
        string nationalId,
        string invalidProperty)
    {
        var act = () => CreateService().CreateAsync(new CreateCustomerRequest(fullName, email, nationalId));

        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Errors.Should().Contain(error => error.PropertyName == invalidProperty);
        _context.VerifyNothingSaved();
    }

    [Fact]
    public async Task CreateAsync_WithExistingEmail_ThrowsConflictException()
    {
        _context.Customers
            .Setup(repository => repository.EmailExistsAsync("jane@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => CreateService().CreateAsync(ValidRequest);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*email*");
        _context.VerifyNothingSaved();
    }

    [Fact]
    public async Task CreateAsync_WithExistingNationalId_ThrowsConflictExceptionWithoutEchoingId()
    {
        _context.Customers
            .Setup(repository => repository.NationalIdExistsAsync("199012345678", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => CreateService().CreateAsync(ValidRequest);

        var exception = (await act.Should().ThrowAsync<ConflictException>()).Which;
        exception.Message.Should().NotContain("199012345678");
        _context.VerifyNothingSaved();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingCustomer_ReturnsResponse()
    {
        var customer = Customer.Create("Jane Doe", "jane@example.com", "AB12", ApplicationTestContext.Now);
        _context.Customers
            .Setup(repository => repository.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var response = await CreateService().GetByIdAsync(customer.Id);

        response.Id.Should().Be(customer.Id);
        response.MaskedNationalId.Should().Be("****");
    }

    [Fact]
    public async Task GetByIdAsync_MissingCustomer_ThrowsNotFoundException()
    {
        var act = () => CreateService().GetByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ListAsync_WithValidPaging_ReturnsMappedPage()
    {
        var customers = new[]
        {
            Customer.Create("Jane Doe", "jane@example.com", "111122223333", ApplicationTestContext.Now),
            Customer.Create("John Roe", "john@example.com", "444455556666", ApplicationTestContext.Now),
        };
        _context.Customers
            .Setup(repository => repository.ListAsync(2, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Customer>(customers, 2, 2, 5));

        var page = await CreateService().ListAsync(new PageRequest { PageNumber = 2, PageSize = 2 });

        page.Items.Select(item => item.Email).Should().Equal("jane@example.com", "john@example.com");
        page.PageNumber.Should().Be(2);
        page.PageSize.Should().Be(2);
        page.TotalCount.Should().Be(5);
        page.TotalPages.Should().Be(3);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, PageRequest.MaxPageSize + 1)]
    public async Task ListAsync_WithInvalidPaging_ThrowsValidationException(int pageNumber, int pageSize)
    {
        var act = () => CreateService().ListAsync(new PageRequest { PageNumber = pageNumber, PageSize = pageSize });

        await act.Should().ThrowAsync<ValidationException>();
        _context.Customers.Verify(
            repository => repository.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
