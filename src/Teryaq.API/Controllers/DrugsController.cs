namespace Teryaq.API.Controllers;

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Teryaq.API.Controllers.Base;
using Teryaq.Application.Common.Pagination;
using Teryaq.Application.Features.Drugs;
using Teryaq.Application.Features.Drugs.Dtos;
using Teryaq.Domain.Features.Drugs;

/// <summary>Manages the global shared Drug catalog.</summary>
[ApiVersion(1)]
public sealed class DrugsController : ApiControllerBase
{
    private readonly IDrugService _drugService;

    /// <summary>Initialises a new instance of <see cref="DrugsController"/>.</summary>
    public DrugsController(IDrugService drugService)
    {
        _drugService = drugService;
    }

    /// <summary>Returns a paginated, filtered list of drugs from the global catalog.</summary>
    [HttpGet]
    [Authorize(Policy = "PharmacyStaff")]
    [ProducesResponseType<PaginatedList<DrugDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPagedAsync(
        [FromQuery] string? search,
        [FromQuery] DrugSource? source,
        [FromQuery] bool? isActive,
        [FromQuery] PaginationParams pagination,
        CancellationToken ct)
    {
        var result = await _drugService.GetPagedAsync(search, source, isActive, pagination, ct);
        return HandleResult(result);
    }

    /// <summary>Returns a single drug by its identifier.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "PharmacyStaff")]
    [ProducesResponseType<DrugDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var result = await _drugService.GetByIdAsync(id, ct);
        return HandleResult(result);
    }

    /// <summary>Creates a new drug in the global catalog.</summary>
    [HttpPost]
    [Authorize(Policy = "OwnerOnly")]
    [ProducesResponseType<DrugDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateDrugRequest request, CancellationToken ct)
    {
        var result = await _drugService.CreateAsync(request, ct);
        object routeValues = result.IsSuccess ? new { id = result.Value.Id } : new { };
        return HandleCreated(result, nameof(GetByIdAsync), routeValues);
    }

    /// <summary>Fully replaces a drug's mutable fields.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "OwnerOnly")]
    [ProducesResponseType<DrugDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateDrugRequest request, CancellationToken ct)
    {
        var result = await _drugService.UpdateAsync(id, request, ct);
        return HandleResult(result);
    }

    /// <summary>Soft-deletes a drug from the global catalog.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "OwnerOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        var result = await _drugService.DeleteAsync(id, ct);
        return HandleDelete(result);
    }
}
