namespace Teryaq.Infrastructure.Features.Users;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Teryaq.Application.Common;
using Teryaq.Application.Common.Tenancy;
using Teryaq.Application.Common.Validation;
using Teryaq.Application.Features.Users;
using Teryaq.Application.Features.Users.Dtos;
using Teryaq.Infrastructure.Identity;

/// <summary>Manages pharmacist users belonging to the current authenticated tenant using ASP.NET Core Identity.</summary>
public sealed class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ICurrentTenant _currentTenant;
    private readonly IValidationService _validationService;

    /// <summary>Initialises a new instance of <see cref="UserService"/>.</summary>
    public UserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ICurrentTenant currentTenant,
        IValidationService validationService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _currentTenant = currentTenant;
        _validationService = validationService;
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<UserDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var users = await _userManager.Users
            .Where(u => u.TenantId == _currentTenant.TenantId)
            .AsNoTracking()
            .ToListAsync(ct);

        var dtos = new List<UserDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            bool isLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;
            dtos.Add(new UserDto(user.Id, user.Email!, user.FullName, roles.FirstOrDefault() ?? "Pharmacist", user.BranchId, isLocked));
        }

        return Result.Ok<IReadOnlyList<UserDto>>(dtos);
    }

    /// <inheritdoc/>
    public async Task<Result<UserDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userManager.Users
            .Where(u => u.Id == id && u.TenantId == _currentTenant.TenantId)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (user is null)
            return Result.Fail<UserDto>(ResultError.NotFound("User", $"User {id} was not found."));

        var roles = await _userManager.GetRolesAsync(user);
        bool isLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;
        return Result.Ok(new UserDto(user.Id, user.Email!, user.FullName, roles.FirstOrDefault() ?? "Pharmacist", user.BranchId, isLocked));
    }

    /// <inheritdoc/>
    public async Task<Result<UserDto>> InviteAsync(InviteUserRequest request, CancellationToken ct = default)
    {
        var validation = await _validationService.ValidateAsync(request, ct);
        if (!validation.IsSuccess)
            return Result.Fail<UserDto>(validation.Error);

        if (await _userManager.FindByEmailAsync(request.Email) is not null)
            return Result.Fail<UserDto>(ResultError.Conflict("User", "A user with this email already exists."));

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            TenantId = _currentTenant.TenantId,
            BranchId = request.BranchId,
            LockoutEnabled = true,
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            string[] errors = createResult.Errors.Select(e => e.Description).ToArray();
            return Result.Fail<UserDto>(ResultError.Validation(new Dictionary<string, string[]> { ["Password"] = errors }));
        }

        const string pharmacistRole = "Pharmacist";
        if (!await _roleManager.RoleExistsAsync(pharmacistRole))
            await _roleManager.CreateAsync(new IdentityRole<Guid>(pharmacistRole) { Id = Guid.NewGuid() });

        await _userManager.AddToRoleAsync(user, pharmacistRole);

        return Result.Ok(new UserDto(user.Id, user.Email!, user.FullName, pharmacistRole, user.BranchId, false));
    }

    /// <inheritdoc/>
    public async Task<Result<UserDto>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var validation = await _validationService.ValidateAsync(request, ct);
        if (!validation.IsSuccess)
            return Result.Fail<UserDto>(validation.Error);

        var user = await _userManager.Users
            .Where(u => u.Id == id && u.TenantId == _currentTenant.TenantId)
            .FirstOrDefaultAsync(ct);

        if (user is null)
            return Result.Fail<UserDto>(ResultError.NotFound("User", $"User {id} was not found."));

        user.FullName = request.FullName;
        user.BranchId = request.BranchId;

        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        bool isLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;
        return Result.Ok(new UserDto(user.Id, user.Email!, user.FullName, roles.FirstOrDefault() ?? "Pharmacist", user.BranchId, isLocked));
    }

    /// <inheritdoc/>
    public async Task<Result<UserDto>> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userManager.Users
            .Where(u => u.Id == id && u.TenantId == _currentTenant.TenantId)
            .FirstOrDefaultAsync(ct);

        if (user is null)
            return Result.Fail<UserDto>(ResultError.NotFound("User", $"User {id} was not found."));

        await _userManager.SetLockoutEnabledAsync(user, true);
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        var roles = await _userManager.GetRolesAsync(user);
        return Result.Ok(new UserDto(user.Id, user.Email!, user.FullName, roles.FirstOrDefault() ?? "Pharmacist", user.BranchId, true));
    }

}
