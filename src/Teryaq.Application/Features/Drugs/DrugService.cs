namespace Teryaq.Application.Features.Drugs;

using AutoMapper;
using Teryaq.Application.Common;
using Teryaq.Application.Common.Pagination;
using Teryaq.Application.Common.Validation;
using Teryaq.Application.Features.Drugs.Dtos;
using Teryaq.Domain.Features.Drugs;
using Teryaq.Domain.Interfaces;

/// <inheritdoc cref="IDrugService"/>
public sealed class DrugService : IDrugService
{
    private readonly IDrugRepository _drugRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidationService _validationService;
    private readonly IMapper _mapper;

    /// <summary>Initialises a new instance of <see cref="DrugService"/>.</summary>
    public DrugService(
        IDrugRepository drugRepository,
        IUnitOfWork unitOfWork,
        IValidationService validationService,
        IMapper mapper)
    {
        _drugRepository = drugRepository;
        _unitOfWork = unitOfWork;
        _validationService = validationService;
        _mapper = mapper;
    }

    /// <inheritdoc/>
    public async Task<Result<PaginatedList<DrugDto>>> GetPagedAsync(
        string? search,
        DrugSource? source,
        bool? isActive,
        PaginationParams pagination,
        CancellationToken ct = default)
    {
        var (items, totalCount) = await _drugRepository.SearchAsync(
            search, source, isActive, pagination.Skip, pagination.PageSize, ct);

        var dtos = _mapper.Map<IReadOnlyList<DrugDto>>(items);
        return Result.Ok(PaginatedList<DrugDto>.Create(dtos, totalCount, pagination.PageNumber, pagination.PageSize));
    }

    /// <inheritdoc/>
    public async Task<Result<DrugDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var drug = await _drugRepository.GetByIdAsync(id, ct);
        if (drug is null)
            return Result.Fail<DrugDto>(ResultError.NotFound<Drug>(id));

        return Result.Ok(_mapper.Map<DrugDto>(drug));
    }

    /// <inheritdoc/>
    public async Task<Result<DrugDto>> CreateAsync(CreateDrugRequest request, CancellationToken ct = default)
    {
        var validation = await _validationService.ValidateAsync(request, ct);
        if (!validation.IsSuccess)
            return Result.Fail<DrugDto>(validation.Error);

        bool duplicate = await _drugRepository.ExistsByNameAndFormAsync(
            request.TradeNameEn, request.Strength, request.DosageForm, excludeId: null, ct);

        if (duplicate)
            return Result.Fail<DrugDto>(ResultError.Conflict("Drug", "A drug with this name, strength, and form already exists."));

        var drug = Drug.Create(
            request.TradeNameAr,
            request.TradeNameEn,
            request.GenericName,
            request.DosageForm,
            request.Strength,
            request.PackSize,
            request.Price,
            request.Barcode,
            request.ManufacturerAr,
            request.ManufacturerEn,
            request.Source);

        await _drugRepository.AddAsync(drug, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Ok(_mapper.Map<DrugDto>(drug));
    }

    /// <inheritdoc/>
    public async Task<Result<DrugDto>> UpdateAsync(Guid id, UpdateDrugRequest request, CancellationToken ct = default)
    {
        var validation = await _validationService.ValidateAsync(request, ct);
        if (!validation.IsSuccess)
            return Result.Fail<DrugDto>(validation.Error);

        var drug = await _drugRepository.GetByIdAsync(id, ct);
        if (drug is null)
            return Result.Fail<DrugDto>(ResultError.NotFound<Drug>(id));

        bool duplicate = await _drugRepository.ExistsByNameAndFormAsync(
            request.TradeNameEn, request.Strength, request.DosageForm, excludeId: id, ct);

        if (duplicate)
            return Result.Fail<DrugDto>(ResultError.Conflict("Drug", "A drug with this name, strength, and form already exists."));

        drug.Update(
            request.TradeNameAr,
            request.TradeNameEn,
            request.GenericName,
            request.DosageForm,
            request.Strength,
            request.PackSize,
            request.Price,
            request.Barcode,
            request.ManufacturerAr,
            request.ManufacturerEn,
            request.IsActive);

        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Ok(_mapper.Map<DrugDto>(drug));
    }

    /// <inheritdoc/>
    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var drug = await _drugRepository.GetByIdAsync(id, ct);
        if (drug is null)
            return Result.Fail(ResultError.NotFound<Drug>(id));

        _drugRepository.Delete(drug);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
