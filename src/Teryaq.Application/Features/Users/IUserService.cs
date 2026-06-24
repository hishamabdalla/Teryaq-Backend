namespace Teryaq.Application.Features.Users;

using Teryaq.Application.Common;
using Teryaq.Application.Features.Users.Dtos;

/// <summary>Manages users (pharmacist staff) belonging to the current authenticated tenant.</summary>
public interface IUserService
{
    /// <summary>Returns all users belonging to the current tenant.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<IReadOnlyList<UserDto>>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns a single user by their identifier.</summary>
    /// <param name="id">User identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<UserDto>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Invites a new Pharmacist user to the current tenant.</summary>
    /// <param name="request">Invitation payload.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<UserDto>> InviteAsync(InviteUserRequest request, CancellationToken ct = default);

    /// <summary>Updates the display name and branch assignment of an existing user.</summary>
    /// <param name="id">User identifier.</param>
    /// <param name="request">Update payload.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<UserDto>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default);

    /// <summary>Deactivates a user account by enabling lockout until far future.</summary>
    /// <param name="id">User identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<UserDto>> DeactivateAsync(Guid id, CancellationToken ct = default);
}
