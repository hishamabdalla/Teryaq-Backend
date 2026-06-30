namespace Teryaq.UnitTests.Features.Sales;

using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Teryaq.Application.Common;
using Teryaq.Application.Common.Tenancy;
using Teryaq.Application.Common.Validation;
using Teryaq.Application.Features.Sales;
using Teryaq.Application.Features.Sales.Dtos;
using Teryaq.Application.Features.Sales.Profiles;
using Teryaq.Domain.Features.Customers;
using Teryaq.Domain.Features.Inventory;
using Teryaq.Domain.Features.Sales;
using Teryaq.Domain.Interfaces;
using Xunit;

/// <summary>Unit tests for <see cref="SaleService"/>.</summary>
public sealed class SaleServiceTests
{
    private readonly IStockBatchRepository _stockBatchRepo = Substitute.For<IStockBatchRepository>();
    private readonly ISaleRepository _saleRepo = Substitute.For<ISaleRepository>();
    private readonly ICustomerRepository _customerRepo = Substitute.For<ICustomerRepository>();
    private readonly IStockMovementRepository _movementRepo = Substitute.For<IStockMovementRepository>();
    private readonly IRepository<SaleLine> _saleLineRepo = Substitute.For<IRepository<SaleLine>>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IValidationService _validation = Substitute.For<IValidationService>();
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();
    private readonly IDateTime _dateTime = Substitute.For<IDateTime>();
    private readonly IMapper _mapper;
    private readonly SaleService _sut;

    private static readonly DateTime FakeNow = new(2026, 6, 30, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly FakeToday = DateOnly.FromDateTime(FakeNow);
    private static readonly Guid FakeUserId = Guid.NewGuid();
    private static readonly Guid FakeBranchId = Guid.NewGuid();
    private static readonly Guid FakeDrugId = Guid.NewGuid();

    /// <summary>Initialises a new instance of <see cref="SaleServiceTests"/>.</summary>
    public SaleServiceTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SaleProfile>(), NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _dateTime.UtcNow.Returns(FakeNow);
        _currentTenant.UserId.Returns(FakeUserId);
        _validation.ValidateAsync(Arg.Any<CreateSaleRequest>(), default).Returns(Result.Ok());

        _sut = new SaleService(
            _stockBatchRepo,
            _saleRepo,
            _customerRepo,
            _movementRepo,
            _saleLineRepo,
            _uow,
            _validation,
            _mapper,
            _currentTenant,
            _dateTime);
    }

    // --- CreateAsync happy path ---

    /// <summary>A single-item sale with sufficient stock creates a sale and dispenses stock.</summary>
    [Fact]
    public async Task CreateAsync_SufficientStock_CreatesSaleAndDispensesStock()
    {
        var batch = MakeBatch(FakeBranchId, FakeDrugId, qty: 10, price: 25m);
        _stockBatchRepo.GetDispensableBatchesAsync(FakeBranchId, FakeDrugId, FakeToday, default)
            .Returns(new List<StockBatch> { batch });

        Sale? capturedSale = null;
        _saleRepo.When(r => r.AddAsync(Arg.Any<Sale>(), default))
            .Do(call => capturedSale = call.Arg<Sale>());
        _saleRepo.GetByIdWithDetailsAsync(Arg.Any<Guid>(), default)
            .Returns(call => Task.FromResult<Sale?>(capturedSale));

        var request = new CreateSaleRequest(FakeBranchId, [new SaleLineRequest(FakeDrugId, 3)], 0m, PaymentMethod.Cash, null);
        var result = await _sut.CreateAsync(request);

        result.IsSuccess.ShouldBeTrue();
        await _uow.Received(1).SaveChangesAsync(default);
        await _saleRepo.Received(1).AddAsync(Arg.Any<Sale>(), default);
        await _saleLineRepo.Received(1).AddAsync(Arg.Any<SaleLine>(), default);
        await _movementRepo.Received(1).AddAsync(Arg.Any<StockMovement>(), default);
        batch.QuantityOnHand.ShouldBe(7);
    }

    /// <summary>When there is not enough stock for a requested quantity, a conflict error is returned.</summary>
    [Fact]
    public async Task CreateAsync_InsufficientStock_ReturnsConflict()
    {
        var batch = MakeBatch(FakeBranchId, FakeDrugId, qty: 2, price: 25m);
        _stockBatchRepo.GetDispensableBatchesAsync(FakeBranchId, FakeDrugId, FakeToday, default)
            .Returns(new List<StockBatch> { batch });

        var request = new CreateSaleRequest(FakeBranchId, [new SaleLineRequest(FakeDrugId, 5)], 0m, PaymentMethod.Cash, null);
        var result = await _sut.CreateAsync(request);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Conflict");
        await _uow.DidNotReceive().SaveChangesAsync(default);
    }

    /// <summary>FEFO: when two batches exist, the oldest-expiry batch is dispensed first.</summary>
    [Fact]
    public async Task CreateAsync_MultiBatch_DispensesOldestExpiryFirst()
    {
        var olderBatch = MakeBatch(FakeBranchId, FakeDrugId, qty: 3, price: 20m, expiryOffset: 10);
        var newerBatch = MakeBatch(FakeBranchId, FakeDrugId, qty: 5, price: 20m, expiryOffset: 60);

        // GetDispensableBatchesAsync returns in FEFO order (oldest first).
        _stockBatchRepo.GetDispensableBatchesAsync(FakeBranchId, FakeDrugId, FakeToday, default)
            .Returns(new List<StockBatch> { olderBatch, newerBatch });

        Sale? capturedSale = null;
        _saleRepo.When(r => r.AddAsync(Arg.Any<Sale>(), default))
            .Do(call => capturedSale = call.Arg<Sale>());
        _saleRepo.GetByIdWithDetailsAsync(Arg.Any<Guid>(), default)
            .Returns(Task.FromResult<Sale?>(capturedSale));

        var request = new CreateSaleRequest(FakeBranchId, [new SaleLineRequest(FakeDrugId, 5)], 0m, PaymentMethod.Cash, null);
        await _sut.CreateAsync(request);

        olderBatch.QuantityOnHand.ShouldBe(0); // fully dispensed first
        newerBatch.QuantityOnHand.ShouldBe(3); // remaining 2 taken from here
    }

    /// <summary>Validation failure returns a validation error without touching repos or UoW.</summary>
    [Fact]
    public async Task CreateAsync_ValidationFails_ReturnsValidationError()
    {
        _validation.ValidateAsync(Arg.Any<CreateSaleRequest>(), default)
            .Returns(Result.Fail(ResultError.Validation(new Dictionary<string, string[]>
            {
                { "Items", ["A sale must contain at least one line item."] },
            })));

        var request = new CreateSaleRequest(FakeBranchId, [], 0m, PaymentMethod.Cash, null);
        var result = await _sut.CreateAsync(request);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Validation");
        await _uow.DidNotReceive().SaveChangesAsync(default);
    }

    /// <summary>When a CustomerId is provided but the customer does not exist, a not-found error is returned.</summary>
    [Fact]
    public async Task CreateAsync_CustomerNotFound_ReturnsNotFound()
    {
        var customerId = Guid.NewGuid();
        _customerRepo.ExistsAsync(customerId, default).Returns(false);

        var request = new CreateSaleRequest(FakeBranchId, [new SaleLineRequest(FakeDrugId, 1)], 0m, PaymentMethod.Cash, customerId);
        var result = await _sut.CreateAsync(request);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("NotFound");
        await _uow.DidNotReceive().SaveChangesAsync(default);
    }

    // --- GetByIdAsync ---

    /// <summary>When the sale does not exist, a not-found result is returned.</summary>
    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNotFound()
    {
        _saleRepo.GetByIdWithDetailsAsync(Arg.Any<Guid>(), default).Returns(Task.FromResult<Sale?>(null));

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("NotFound");
    }

    // --- Helpers ---

    private static StockBatch MakeBatch(Guid branchId, Guid drugId, int qty, decimal price, int expiryOffset = 30) =>
        StockBatch.Create(
            branchId,
            drugId,
            $"LOT-{expiryOffset:000}",
            FakeToday.AddDays(expiryOffset),
            qty,
            reorderLevel: 0,
            costPrice: price * 0.6m,
            sellingPrice: price,
            receivedAt: DateTime.UtcNow);
}
