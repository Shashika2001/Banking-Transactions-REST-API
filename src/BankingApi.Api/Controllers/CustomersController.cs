using BankingApi.Application.Common.Models;
using BankingApi.Application.Common.Security;
using BankingApi.Application.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingApi.Api.Controllers;

/// <summary>
/// Creates and reads customers.
/// </summary>
[ApiController]
[Route("api/customers")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomersController"/> class.
    /// </summary>
    /// <param name="customerService">The customer service.</param>
    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>
    /// Creates a customer (admin only). Customers sign up through <c>POST /api/auth/register</c>.
    /// </summary>
    /// <param name="request">The customer details.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The created customer.</returns>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerResponse>> Create(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = await _customerService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { customerId = customer.Id }, customer);
    }

    /// <summary>
    /// Gets a customer. Customers may only get themselves.
    /// </summary>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The customer.</returns>
    [HttpGet("{customerId:guid}")]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerResponse>> GetById(Guid customerId, CancellationToken cancellationToken) =>
        await _customerService.GetByIdAsync(customerId, cancellationToken);

    /// <summary>
    /// Lists customers, oldest first (admin only).
    /// </summary>
    /// <param name="request">Paging parameters (<c>pageNumber</c>, <c>pageSize</c> up to 100).</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>One page of customers.</returns>
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<PagedResult<CustomerResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<CustomerResponse>>> List(
        [FromQuery] PageRequest request,
        CancellationToken cancellationToken) =>
        await _customerService.ListAsync(request, cancellationToken);
}
