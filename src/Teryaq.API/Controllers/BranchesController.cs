namespace Teryaq.API.Controllers;

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Teryaq.API.Controllers.Base;
using Teryaq.Application.Features.Branches;
using Teryaq.Application.Features.Branches.Dtos;

/// <summary>Manages the physical branches of the current authenticated tenant.</summary>
[ApiVersion(1)]
[Authorize(Policy = "OwnerOnly")]
public sealed class BranchesController : ApiControllerBase
{
    private readonly IBranchService _branchService;

    /// <summary>Initialises a new instance of <see cref="BranchesController"/>.</summary>
    public BranchesController(IBranchService branchService)
    {
        _branchService = branchService;
    }

    /// <summary>Returns all branches belonging to the current tenant.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BranchDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync(CancellationToken ct)
    {
        var result = await _branchService.GetAllAsync(ct);
        return HandleResult(result);
    }

    /// <summary>Returns a single branch by its identifier.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<BranchDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var result = await _branchService.GetByIdAsync(id, ct);
        return HandleResult(result);
    }

    /// <summary>Creates a new branch under the current tenant.</summary>
    [HttpPost]
    [ProducesResponseType<BranchDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateBranchRequest request, CancellationToken ct)
    {
        var result = await _branchService.CreateAsync(request, ct);
        object routeValues = result.IsSuccess ? new { id = result.Value.Id } : new { };
        return HandleCreated(result, nameof(GetByIdAsync), routeValues);
    }

    /// <summary>Updates the mutable fields of an existing branch.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<BranchDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateBranchRequest request, CancellationToken ct)
    {
        var result = await _branchService.UpdateAsync(id, request, ct);
        return HandleResult(result);
    }

    /// <summary>Soft-deletes a branch. The primary branch cannot be deleted.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        var result = await _branchService.DeleteAsync(id, ct);
        return HandleDelete(result);
    }

    /// <summary>Deactivates a branch, preventing new operations from being assigned to it.</summary>
    [HttpPut("{id:guid}/deactivate")]
    [ProducesResponseType<BranchDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateAsync(Guid id, CancellationToken ct)
    {
        var result = await _branchService.DeactivateAsync(id, ct);
        return HandleResult(result);
    }
}
