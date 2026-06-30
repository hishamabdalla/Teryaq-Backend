namespace Teryaq.Application.Features.Sales;

using AutoMapper;
using Teryaq.Application.Common;
using Teryaq.Application.Common.Tenancy;
using Teryaq.Application.Common.Validation;
using Teryaq.Application.Features.Sales.Dtos;
using Teryaq.Domain.Features.Customers;
using Teryaq.Domain.Features.Inventory;
using Teryaq.Domain.Features.Sales;
using Teryaq.Domain.Interfaces;
using ISaleLineRepo = Teryaq.Domain.Interfaces.IRepository<Teryaq.Domain.Features.Sales.SaleLine>;

/// <inheritdoc cref="ISaleService"/>
public sealed class SaleService : ISaleService
{
    private readonly IStockBatchRepository _stockBatchRepository;
    private readonly ISaleRepository _saleRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly ISaleLineRepo _saleLineRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidationService _validationService;
    private readonly IMapper _mapper;
    private readonly ICurrentTenant _currentTenant;
    private readonly IDateTime _dateTime;

    /// <summary>Initialises a new instance of <see cref="SaleService"/>.</summary>
    public SaleService(
        IStockBatchRepository stockBatchRepository,
        ISaleRepository saleRepository,
        ICustomerRepository customerRepository,
        IStockMovementRepository stockMovementRepository,
        ISaleLineRepo saleLineRepository,
        IUnitOfWork unitOfWork,
        IValidationService validationService,
        IMapper mapper,
        ICurrentTenant currentTenant,
        IDateTime dateTime)
    {
        _stockBatchRepository = stockBatchRepository;
        _saleRepository = saleRepository;
        _customerRepository = customerRepository;
        _stockMovementRepository = stockMovementRepository;
        _saleLineRepository = saleLineRepository;
        _unitOfWork = unitOfWork;
        _validationService = validationService;
        _mapper = mapper;
        _currentTenant = currentTenant;
        _dateTime = dateTime;
    }

    /// <inheritdoc/>
    public async Task<Result<SaleDto>> CreateAsync(CreateSaleRequest request, CancellationToken ct = default)
    {
        var validation = await _validationService.ValidateAsync(request, ct);
        if (!validation.IsSuccess)
            return Result.Fail<SaleDto>(validation.Error);

        if (request.CustomerId.HasValue)
        {
            bool customerExists = await _customerRepository.ExistsAsync(request.CustomerId.Value, ct);
            if (!customerExists)
                return Result.Fail<SaleDto>(ResultError.NotFound<Customer>(request.CustomerId.Value));
        }

        var cashierUserId = _currentTenant.UserId ?? Guid.Empty;
        var now = _dateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        // FEFO allocation: collect (batchId, drugId, dispenseQty, unitPrice) per line.
        var allocations = new List<(Guid BatchId, Guid DrugId, int Qty, decimal UnitPrice)>();
        decimal total = 0m;

        foreach (var item in request.Items)
        {
            var batches = await _stockBatchRepository.GetDispensableBatchesAsync(request.BranchId, item.DrugId, today, ct);

            int remaining = item.Quantity;
            int totalAvailable = 0;
            foreach (var batch in batches)
                totalAvailable += batch.QuantityOnHand;

            if (totalAvailable < item.Quantity)
                return Result.Fail<SaleDto>(ResultError.Conflict(
                    "Stock",
                    $"Insufficient stock for drug {item.DrugId}. Requested {item.Quantity}, available {totalAvailable}."));

            // Dispense from oldest-expiry batches first (FEFO).
            foreach (var batch in batches)
            {
                if (remaining <= 0)
                    break;

                int dispenseQty = Math.Min(remaining, batch.QuantityOnHand);
                batch.Dispense(dispenseQty);
                allocations.Add((batch.Id, item.DrugId, dispenseQty, batch.SellingPrice));
                total += dispenseQty * batch.SellingPrice;
                remaining -= dispenseQty;
            }
        }

        string saleNumber = $"TRQ-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        var sale = Sale.Create(request.BranchId, cashierUserId, saleNumber, total, request.Discount, request.PaymentMethod, now, request.CustomerId);
        await _saleRepository.AddAsync(sale, ct);

        // Create sale lines and stock movement audit records.
        foreach (var (batchId, drugId, qty, unitPrice) in allocations)
        {
            var line = SaleLine.Create(sale.Id, drugId, batchId, qty, unitPrice);
            await _saleLineRepository.AddAsync(line, ct);

            var movement = StockMovement.Create(
                request.BranchId,
                drugId,
                batchId,
                StockMovementType.Dispense,
                qty,
                cashierUserId,
                sale.Id,
                reason: null);
            await _stockMovementRepository.AddAsync(movement, ct);
        }

        // Single SaveChanges — atomically commits batch decrements, sale, lines, and movements.
        await _unitOfWork.SaveChangesAsync(ct);

        var saved = await _saleRepository.GetByIdWithDetailsAsync(sale.Id, ct);
        return Result.Ok(_mapper.Map<SaleDto>(saved!));
    }

    /// <inheritdoc/>
    public async Task<Result<SaleDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var sale = await _saleRepository.GetByIdWithDetailsAsync(id, ct);
        if (sale is null)
            return Result.Fail<SaleDto>(ResultError.NotFound<Sale>(id));
        return Result.Ok(_mapper.Map<SaleDto>(sale));
    }

    /// <inheritdoc/>
    public async Task<Result<List<TodaySaleSummaryDto>>> GetTodaysAsync(Guid? branchId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(_dateTime.UtcNow);
        var sales = await _saleRepository.GetTodaysSalesAsync(branchId, today, ct);
        return Result.Ok(_mapper.Map<List<TodaySaleSummaryDto>>(sales));
    }
}
