namespace Teryaq.Infrastructure.Features.Auth;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Teryaq.Application.Common;
using Teryaq.Application.Common.Tokens;
using Teryaq.Application.Common.Validation;
using Teryaq.Application.Features.Auth;
using Teryaq.Application.Features.Auth.Dtos;
using Teryaq.Domain.Features.Branches;
using Teryaq.Domain.Features.Tenants;
using Teryaq.Domain.Interfaces;
using Teryaq.Infrastructure.Identity;
using Teryaq.Infrastructure.Persistence;

/// <summary>Handles tenant registration, login, and token refresh using ASP.NET Core Identity.</summary>
public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ITenantRepository _tenantRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IValidationService _validationService;
    private readonly AppDbContext _context;

    /// <summary>Initialises a new instance of <see cref="AuthService"/>.</summary>
    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ITenantRepository tenantRepository,
        IBranchRepository branchRepository,
        IUnitOfWork unitOfWork,
        IJwtTokenGenerator jwtTokenGenerator,
        IValidationService validationService,
        AppDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tenantRepository = tenantRepository;
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
        _jwtTokenGenerator = jwtTokenGenerator;
        _validationService = validationService;
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var validation = await _validationService.ValidateAsync(request, ct);
        if (!validation.IsSuccess) return Result.Fail<AuthResponse>(validation.Error);

        if (await _userManager.FindByEmailAsync(request.OwnerEmail) is not null)
            return Result.Fail<AuthResponse>(ResultError.Conflict("User", "Email is already registered."));

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        var tenant = Tenant.Create(request.PharmacyName);
        await _tenantRepository.AddAsync(tenant, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var branch = Branch.CreateMain(tenant.Id, request.BranchName, request.BranchAddress, request.BranchPhone);
        await _branchRepository.AddAsync(branch, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        const string ownerRole = "Owner";
        await EnsureRoleExistsAsync(ownerRole);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.OwnerEmail,
            Email = request.OwnerEmail,
            TenantId = tenant.Id,
            BranchId = null,
        };

        var createResult = await _userManager.CreateAsync(user, request.OwnerPassword);
        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync(ct);
            string errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            return Result.Fail<AuthResponse>(ResultError.Validation(errors));
        }

        await _userManager.AddToRoleAsync(user, ownerRole);

        var (accessToken, refreshToken, expiresAt) = IssueTokens(user, ownerRole);
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        await transaction.CommitAsync(ct);

        return Result.Ok(new AuthResponse(accessToken, refreshToken, expiresAt, tenant.Id, null, ownerRole));
    }

    /// <inheritdoc/>
    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var validation = await _validationService.ValidateAsync(request, ct);
        if (!validation.IsSuccess) return Result.Fail<AuthResponse>(validation.Error);

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return Result.Fail<AuthResponse>(ResultError.Failure("User", "Invalid email or password."));

        var roles = await _userManager.GetRolesAsync(user);
        string role = roles.FirstOrDefault() ?? "Pharmacist";

        var (accessToken, refreshToken, expiresAt) = IssueTokens(user, role);
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        return Result.Ok(new AuthResponse(accessToken, refreshToken, expiresAt, user.TenantId, user.BranchId, role));
    }

    /// <inheritdoc/>
    public async Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, ct);

        if (user is null || user.RefreshTokenExpiry < DateTime.UtcNow)
            return Result.Fail<AuthResponse>(ResultError.Failure("Token", "Invalid or expired refresh token."));

        var roles = await _userManager.GetRolesAsync(user);
        string role = roles.FirstOrDefault() ?? "Pharmacist";

        var (accessToken, refreshToken, expiresAt) = IssueTokens(user, role);
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        return Result.Ok(new AuthResponse(accessToken, refreshToken, expiresAt, user.TenantId, user.BranchId, role));
    }

    private (string AccessToken, string RefreshToken, DateTime ExpiresAt) IssueTokens(ApplicationUser user, string role)
    {
        string accessToken = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.TenantId, user.BranchId, role);
        string refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(60);
        return (accessToken, refreshToken, expiresAt);
    }

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
            await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName) { Id = Guid.NewGuid() });
    }
}
