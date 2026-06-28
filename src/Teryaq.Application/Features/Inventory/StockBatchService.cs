namespace Teryaq.Application.Features.Inventory;

using AutoMapper;
using Teryaq.Application.Common;
using Teryaq.Application.Common.Pagination;
using Teryaq.Application.Common.Validation;
using Teryaq.Application.Features.Inventory.Dtos;
using Teryaq.Domain.Features.Branches;
using Teryaq.Domain.Features.Drugs;
using Teryaq.Domain.Features.Inventory;
using Teryaq.Domain.Interfaces;

/// <inheritdoc cref="IStockBatchService"/>
public sealed class StockBatchService : IStockBatchService
{
    private readonly IStockBatchRepository _stockBatchRepository;
    private readonly IDrugRepository _drugRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidationService _validationService;
    private readonly IMapper _mapper;

    /// <summary>Initialises a new instance of <see cref="StockBatchService"/>.</summary>
    public StockBatchService(
        IStockBatchRepository stockBatchRepository,
        IDrugRepository drugRepository,
        IBranchRepository branchRepository,
        IUnitOfWork unitOfWork,
        IValidationService validationService,
        IMapper mapper)
    {
        _stockBatchRepository = stockBatchRepository;
        _drugRepository = drugRepository;
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
        _validationService = validationService;
        _mapper = mapper;
    }

    /// <inheritdoc/>
    public async Task<Result<PaginatedList<StockBatchDto>>> GetPagedAsync(
        Guid? branchId,
        Guid? drugId,
        DateOnly? expiringBefore,
        string? search,
        PaginationParams pagination,
        CancellationToken ct = default)
    {
        var (items, totalCount) = await _stockBatchRepository.SearchAsync(
            branchId, drugId, expiringBefore, search, pagination.Skip, pagination.PageSize, ct);

        var dtos = _mapper.Map<IReadOnlyList<StockBatchDto>>(items);
        return Result.Ok(PaginatedList<StockBatchDto>.Create(dtos, totalCount, pagination.PageNumber, pagination.PageSize));
    }

    /// <inheritdoc/>
    public async Task<Result<StockBatchDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var batch = await _stockBatchRepository.GetByIdWithDetailsAsync(id, ct);
        if (batch is null)
            return Result.Fail<StockBatchDto>(ResultError.NotFound<StockBatch>(id));

        return Result.Ok(_mapper.Map<StockBatchDto>(batch));
    }

    /// <inheritdoc/>
    public async Task<Result<StockBatchDto>> ReceiveAsync(ReceiveStockRequest request, CancellationToken ct = default)
    {
        var validation = await _validationService.ValidateAsync(request, ct);
        if (!validation.IsSuccess)
            return Result.Fail<StockBatchDto>(validation.Error);

        var drug = await _drugRepository.GetByIdAsync(request.DrugId, ct);
        if (drug is null)
            return Result.Fail<StockBatchDto>(ResultError.NotFound<Drug>(request.DrugId));

        if (!drug.IsActive)
            return Result.Fail<StockBatchDto>(ResultError.Conflict("Drug", "Cannot receive stock for an inactive drug."));

        var branch = await _branchRepository.GetByIdAsync(request.BranchId, ct);
        if (branch is null)
            return Result.Fail<StockBatchDto>(ResultError.NotFound<Branch>(request.BranchId));

        decimal sellingPrice = request.SellingPrice ?? drug.Price;

        var batch = StockBatch.Create(
            request.BranchId,
            request.DrugId,
            request.BatchNumber,
            request.ExpiryDate,
            request.Quantity,
            request.ReorderLevel,
            request.CostPrice,
            sellingPrice,
            DateTime.UtcNow);

        await _stockBatchRepository.AddAsync(batch, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Reload with navigation properties — the just-saved entity has no navs populated in memory.
        var saved = await _stockBatchRepository.GetByIdWithDetailsAsync(batch.Id, ct);
        return Result.Ok(_mapper.Map<StockBatchDto>(saved!));
    }

    /// <inheritdoc/>
    public async Task<Result<StockBatchDto>> AdjustAsync(Guid id, AdjustStockRequest request, CancellationToken ct = default)
    {
        var validation = await _validationService.ValidateAsync(request, ct);
        if (!validation.IsSuccess)
            return Result.Fail<StockBatchDto>(validation.Error);

        // Load tracked (no nav props) for the mutation.
        var batch = await _stockBatchRepository.GetByIdAsync(id, ct);
        if (batch is null)
            return Result.Fail<StockBatchDto>(ResultError.NotFound<StockBatch>(id));

        batch.Adjust(request.QuantityOnHand, request.SellingPrice, request.ExpiryDate, request.ReorderLevel);
        await _unitOfWork.SaveChangesAsync(ct);

        // Reload with nav props for the response DTO.
        var updated = await _stockBatchRepository.GetByIdWithDetailsAsync(id, ct);
        return Result.Ok(_mapper.Map<StockBatchDto>(updated!));
    }

    /// <inheritdoc/>
    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var batch = await _stockBatchRepository.GetByIdAsync(id, ct);
        if (batch is null)
            return Result.Fail(ResultError.NotFound<StockBatch>(id));

        _stockBatchRepository.Delete(batch);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
