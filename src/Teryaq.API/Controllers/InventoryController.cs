namespace Teryaq.API.Controllers;

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Teryaq.API.Controllers.Base;
using Teryaq.Application.Common.Pagination;
using Teryaq.Application.Features.Inventory;
using Teryaq.Application.Features.Inventory.Dtos;

/// <summary>Manages stock batches held in branch inventories for the current tenant.</summary>
[ApiVersion(1)]
[Authorize(Policy = "PharmacyStaff")]
public sealed class InventoryController : ApiControllerBase
{
    private readonly IStockBatchService _stockBatchService;

    /// <summary>Initialises a new instance of <see cref="InventoryController"/>.</summary>
    public InventoryController(IStockBatchService stockBatchService)
    {
        _stockBatchService = stockBatchService;
    }

    /// <summary>Returns a paginated, filtered list of stock batches for the current tenant.</summary>
    [HttpGet]
    [ProducesResponseType<PaginatedList<StockBatchDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? drugId,
        [FromQuery] DateOnly? expiringBefore,
        [FromQuery] string? search,
        [FromQuery] PaginationParams pagination,
        CancellationToken ct)
    {
        var result = await _stockBatchService.GetPagedAsync(branchId, drugId, expiringBefore, search, pagination, ct);
        return HandleResult(result);
    }

    /// <summary>Returns a single stock batch by its identifier.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<StockBatchDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var result = await _stockBatchService.GetByIdAsync(id, ct);
        return HandleResult(result);
    }

    /// <summary>Records a new stock batch received from a supplier.</summary>
    [HttpPost]
    [ProducesResponseType<StockBatchDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ReceiveAsync([FromBody] ReceiveStockRequest request, CancellationToken ct)
    {
        var result = await _stockBatchService.ReceiveAsync(request, ct);
        object routeValues = result.IsSuccess ? new { id = result.Value.Id } : new { };
        return HandleCreated(result, nameof(GetByIdAsync), routeValues);
    }

    /// <summary>Applies a correction to an existing stock batch (quantity, price, or expiry date).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<StockBatchDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AdjustAsync(Guid id, [FromBody] AdjustStockRequest request, CancellationToken ct)
    {
        var result = await _stockBatchService.AdjustAsync(id, request, ct);
        return HandleResult(result);
    }

    /// <summary>Soft-deletes (writes off) a stock batch.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        var result = await _stockBatchService.DeleteAsync(id, ct);
        return HandleDelete(result);
    }
}
