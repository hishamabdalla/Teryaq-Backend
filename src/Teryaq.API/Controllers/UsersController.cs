namespace Teryaq.API.Controllers;

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Teryaq.API.Controllers.Base;
using Teryaq.Application.Features.Users;
using Teryaq.Application.Features.Users.Dtos;

/// <summary>Manages pharmacist staff users belonging to the current authenticated tenant.</summary>
[ApiVersion(1)]
[Authorize(Policy = "OwnerOnly")]
public sealed class UsersController : ApiControllerBase
{
    private readonly IUserService _userService;

    /// <summary>Initialises a new instance of <see cref="UsersController"/>.</summary>
    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>Returns all users belonging to the current tenant.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync(CancellationToken ct)
    {
        var result = await _userService.GetAllAsync(ct);
        return HandleResult(result);
    }

    /// <summary>Returns a single user by their identifier.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var result = await _userService.GetByIdAsync(id, ct);
        return HandleResult(result);
    }

    /// <summary>Invites a new Pharmacist user to the current tenant.</summary>
    [HttpPost]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateAsync([FromBody] InviteUserRequest request, CancellationToken ct)
    {
        var result = await _userService.InviteAsync(request, ct);
        object routeValues = result.IsSuccess ? new { id = result.Value.Id } : new { };
        return HandleCreated(result, nameof(GetByIdAsync), routeValues);
    }

    /// <summary>Updates the display name and branch assignment of an existing user.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var result = await _userService.UpdateAsync(id, request, ct);
        return HandleResult(result);
    }

    /// <summary>Deactivates a user account, preventing future logins.</summary>
    [HttpPut("{id:guid}/deactivate")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateAsync(Guid id, CancellationToken ct)
    {
        var result = await _userService.DeactivateAsync(id, ct);
        return HandleResult(result);
    }
}
