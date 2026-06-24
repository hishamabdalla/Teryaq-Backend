namespace Teryaq.Application.Features.Branches;

using AutoMapper;
using Teryaq.Application.Common;
using Teryaq.Application.Common.Validation;
using Teryaq.Application.Features.Branches.Dtos;
using Teryaq.Domain.Features.Branches;
using Teryaq.Domain.Interfaces;

/// <inheritdoc cref="IBranchService"/>
public sealed class BranchService : IBranchService
{
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidationService _validationService;
    private readonly IMapper _mapper;

    /// <summary>Initialises a new instance of <see cref="BranchService"/>.</summary>
    public BranchService(
        IBranchRepository branchRepository,
        IUnitOfWork unitOfWork,
        IValidationService validationService,
        IMapper mapper)
    {
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
        _validationService = validationService;
        _mapper = mapper;
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<BranchDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var branches = await _branchRepository.GetAllAsync(ct);
        return Result.Ok(_mapper.Map<IReadOnlyList<BranchDto>>(branches));
    }

    /// <inheritdoc/>
    public async Task<Result<BranchDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var branch = await _branchRepository.GetByIdAsync(id, ct);
        if (branch is null)
            return Result.Fail<BranchDto>(ResultError.NotFound<Branch>(id));

        return Result.Ok(_mapper.Map<BranchDto>(branch));
    }

    /// <inheritdoc/>
    public async Task<Result<BranchDto>> CreateAsync(CreateBranchRequest request, CancellationToken ct = default)
    {
        var validation = await _validationService.ValidateAsync(request, ct);
        if (!validation.IsSuccess)
            return Result.Fail<BranchDto>(validation.Error);

        bool duplicate = await _branchRepository.ExistsByNameAsync(request.Name, excludeId: null, ct);
        if (duplicate)
            return Result.Fail<BranchDto>(ResultError.Conflict("Branch", "A branch with this name already exists in your pharmacy."));

        var branch = Branch.Create(Guid.Empty, request.Name, request.Address, request.Phone);
        // TenantId is stamped by TenantInterceptor on insert — do not set manually.

        await _branchRepository.AddAsync(branch, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Ok(_mapper.Map<BranchDto>(branch));
    }

    /// <inheritdoc/>
    public async Task<Result<BranchDto>> UpdateAsync(Guid id, UpdateBranchRequest request, CancellationToken ct = default)
    {
        var validation = await _validationService.ValidateAsync(request, ct);
        if (!validation.IsSuccess)
            return Result.Fail<BranchDto>(validation.Error);

        var branch = await _branchRepository.GetByIdAsync(id, ct);
        if (branch is null)
            return Result.Fail<BranchDto>(ResultError.NotFound<Branch>(id));

        bool duplicate = await _branchRepository.ExistsByNameAsync(request.Name, excludeId: id, ct);
        if (duplicate)
            return Result.Fail<BranchDto>(ResultError.Conflict("Branch", "A branch with this name already exists in your pharmacy."));

        branch.Update(request.Name, request.Address, request.Phone);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Ok(_mapper.Map<BranchDto>(branch));
    }

    /// <inheritdoc/>
    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var branch = await _branchRepository.GetByIdAsync(id, ct);
        if (branch is null)
            return Result.Fail(ResultError.NotFound<Branch>(id));

        if (branch.IsMain)
            return Result.Fail(ResultError.Conflict("Branch", "The primary branch cannot be deleted."));

        _branchRepository.Delete(branch);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }

    /// <inheritdoc/>
    public async Task<Result<BranchDto>> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var branch = await _branchRepository.GetByIdAsync(id, ct);
        if (branch is null)
            return Result.Fail<BranchDto>(ResultError.NotFound<Branch>(id));

        if (branch.IsMain)
            return Result.Fail<BranchDto>(ResultError.Conflict("Branch", "The primary branch cannot be deactivated."));

        branch.Deactivate();
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Ok(_mapper.Map<BranchDto>(branch));
    }
}
