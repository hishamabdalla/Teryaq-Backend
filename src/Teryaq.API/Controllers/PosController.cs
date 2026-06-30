namespace Teryaq.API.Controllers;

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Teryaq.API.Controllers.Base;
using Teryaq.Application.Features.Sales;
using Teryaq.Application.Features.Sales.Dtos;

/// <summary>POS sale endpoints — creates and retrieves point-of-sale transactions.</summary>
[ApiVersion(1)]
[Authorize(Policy = "PharmacyStaff")]
[Route("api/v{version:apiVersion}/pos/sales")]
public sealed class PosController : ApiControllerBase
{
    private readonly ISaleService _saleService;

    /// <summary>Initialises a new instance of <see cref="PosController"/>.</summary>
    public PosController(ISaleService saleService)
    {
        _saleService = saleService;
    }

    /// <summary>Returns today's sales for the current tenant, optionally filtered by branch.</summary>
    [HttpGet("today")]
    [ProducesResponseType<List<TodaySaleSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTodaysAsync([FromQuery] Guid? branchId, CancellationToken ct)
    {
        var result = await _saleService.GetTodaysAsync(branchId, ct);
        return HandleResult(result);
    }

    /// <summary>Returns a single sale by identifier with all dispensed line items.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<SaleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var result = await _saleService.GetByIdAsync(id, ct);
        return HandleResult(result);
    }

    /// <summary>Confirms a POS sale: allocates stock via FEFO, decrements batches, writes audit records, and persists the sale.</summary>
    [HttpPost]
    [ProducesResponseType<SaleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateSaleRequest request, CancellationToken ct)
    {
        var result = await _saleService.CreateAsync(request, ct);
        return HandleCreated(result, nameof(GetByIdAsync), result.IsSuccess ? new { id = result.Value.Id } : new { });
    }
}
