namespace Teryaq.API.Controllers;

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Teryaq.API.Controllers.Base;
using Teryaq.Application.Common.Pagination;
using Teryaq.Application.Features.Customers;
using Teryaq.Application.Features.Customers.Dtos;

/// <summary>Manages customer profiles for the current tenant.</summary>
[ApiVersion(1)]
[Authorize(Policy = "PharmacyStaff")]
public sealed class CustomersController : ApiControllerBase
{
    private readonly ICustomerService _customerService;

    /// <summary>Initialises a new instance of <see cref="CustomersController"/>.</summary>
    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>Returns a paginated list of customers for the current tenant.</summary>
    [HttpGet]
    [ProducesResponseType<PaginatedList<CustomerDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPagedAsync(
        [FromQuery] string? search,
        [FromQuery] PaginationParams pagination,
        CancellationToken ct)
    {
        var result = await _customerService.GetPagedAsync(search, pagination, ct);
        return HandleResult(result);
    }

    /// <summary>Returns a single customer by identifier.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var result = await _customerService.GetByIdAsync(id, ct);
        return HandleResult(result);
    }

    /// <summary>Registers a new customer for the current tenant.</summary>
    [HttpPost]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        var result = await _customerService.CreateAsync(request, ct);
        return HandleCreated(result, nameof(GetByIdAsync), result.IsSuccess ? new { id = result.Value.Id } : new { });
    }
}
