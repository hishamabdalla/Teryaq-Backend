namespace Teryaq.UnitTests.Features.Inventory;

using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Teryaq.Application.Common;
using Teryaq.Application.Common.Pagination;
using Teryaq.Application.Common.Validation;
using Teryaq.Application.Features.Inventory;
using Teryaq.Application.Features.Inventory.Dtos;
using Teryaq.Application.Features.Inventory.Profiles;
using Teryaq.Domain.Features.Branches;
using Teryaq.Domain.Features.Drugs;
using Teryaq.Domain.Features.Inventory;
using Teryaq.Domain.Interfaces;
using Xunit;

/// <summary>Unit tests for <see cref="StockBatchService"/>.</summary>
public sealed class StockBatchServiceTests
{
    private readonly IStockBatchRepository _repo = Substitute.For<IStockBatchRepository>();
    private readonly IDrugRepository _drugRepo = Substitute.For<IDrugRepository>();
    private readonly IBranchRepository _branchRepo = Substitute.For<IBranchRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IValidationService _validation = Substitute.For<IValidationService>();
    private readonly IMapper _mapper;
    private readonly StockBatchService _sut;

    private static readonly Drug ActiveDrug = Drug.Create(
        "باراسيتامول", "Paracetamol", "Paracetamol", "Tablet", "500mg",
        20, 15.00m, null, null, null, DrugSource.Manual);

    private static readonly Branch TestBranch =
        Branch.Create(Guid.NewGuid(), "Main Branch", "123 Cairo St", "010-0000001");

    /// <summary>Initialises a new instance of <see cref="StockBatchServiceTests"/>.</summary>
    public StockBatchServiceTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StockBatchProfile>(), NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _sut = new StockBatchService(_repo, _drugRepo, _branchRepo, _uow, _validation, _mapper);
    }

    // --- GetByIdAsync ---

    /// <summary>Batch exists → returns the mapped DTO.</summary>
    [Fact]
    public async Task GetByIdAsync_WhenBatchExists_ReturnsDto()
    {
        var batch = MakeBatch();
        _repo.GetByIdWithDetailsAsync(batch.Id, default).Returns(batch);

        var result = await _sut.GetByIdAsync(batch.Id);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(batch.Id);
        result.Value.BatchNumber.ShouldBe("LOT-001");
    }

    /// <summary>Batch not found → returns NotFound error.</summary>
    [Fact]
    public async Task GetByIdAsync_WhenBatchNotFound_ReturnsNotFoundError()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdWithDetailsAsync(id, default).Returns((StockBatch?)null);

        var result = await _sut.GetByIdAsync(id);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(ResultErrorCodes.NotFound);
    }

    // --- ReceiveAsync ---

    /// <summary>Valid receive request → saves batch and returns DTO.</summary>
    [Fact]
    public async Task ReceiveAsync_WhenValid_SavesBatchAndReturnsDto()
    {
        var request = new ReceiveStockRequest(
            TestBranch.Id, ActiveDrug.Id, "LOT-001",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            100, 0, 10.00m, null);

        var savedBatch = MakeBatch();

        _validation.ValidateAsync(request, default).Returns(Result.Ok());
        _drugRepo.GetByIdAsync(ActiveDrug.Id, default).Returns(ActiveDrug);
        _branchRepo.GetByIdAsync(TestBranch.Id, default).Returns(TestBranch);
        _repo.GetByIdWithDetailsAsync(Arg.Any<Guid>(), default).Returns(savedBatch);

        var result = await _sut.ReceiveAsync(request);

        result.IsSuccess.ShouldBeTrue();
        await _uow.Received(1).SaveChangesAsync(default);
        await _repo.Received(1).AddAsync(Arg.Any<StockBatch>(), default);
    }

    /// <summary>Selling price defaults to drug catalog price when not provided.</summary>
    [Fact]
    public async Task ReceiveAsync_WhenSellingPriceNull_DefaultsToDrugPrice()
    {
        var request = new ReceiveStockRequest(
            TestBranch.Id, ActiveDrug.Id, "LOT-002",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            50, 0, 8.00m, null);  // SellingPrice = null

        var capturedBatch = (StockBatch?)null;

        _validation.ValidateAsync(request, default).Returns(Result.Ok());
        _drugRepo.GetByIdAsync(ActiveDrug.Id, default).Returns(ActiveDrug);
        _branchRepo.GetByIdAsync(TestBranch.Id, default).Returns(TestBranch);
        _repo.When(r => r.AddAsync(Arg.Any<StockBatch>(), default))
            .Do(call => capturedBatch = call.Arg<StockBatch>());
        _repo.GetByIdWithDetailsAsync(Arg.Any<Guid>(), default).Returns(MakeBatch());

        await _sut.ReceiveAsync(request);

        capturedBatch.ShouldNotBeNull();
        capturedBatch!.SellingPrice.ShouldBe(ActiveDrug.Price);
    }

    /// <summary>Drug not found → returns NotFound error without saving.</summary>
    [Fact]
    public async Task ReceiveAsync_WhenDrugNotFound_ReturnsNotFoundError()
    {
        var request = new ReceiveStockRequest(
            TestBranch.Id, Guid.NewGuid(), "LOT-003",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            10, 0, 5.00m, null);

        _validation.ValidateAsync(request, default).Returns(Result.Ok());
        _drugRepo.GetByIdAsync(request.DrugId, default).Returns((Drug?)null);

        var result = await _sut.ReceiveAsync(request);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(ResultErrorCodes.NotFound);
        await _uow.DidNotReceive().SaveChangesAsync(default);
    }

    /// <summary>Drug is inactive → returns Conflict error without saving.</summary>
    [Fact]
    public async Task ReceiveAsync_WhenDrugInactive_ReturnsConflictError()
    {
        // Create an inactive drug (requires Update to set IsActive=false)
        var inactiveDrug = Drug.Create(
            "Inactive Drug", "Inactive Drug EN", "Generic", "Tablet", "100mg",
            10, 5.00m, null, null, null, DrugSource.Manual);
        inactiveDrug.Update("Inactive Drug", "Inactive Drug EN", "Generic", "Tablet", "100mg",
            10, 5.00m, null, null, null, isActive: false);

        var request = new ReceiveStockRequest(
            TestBranch.Id, inactiveDrug.Id, "LOT-004",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            10, 0, 5.00m, null);

        _validation.ValidateAsync(request, default).Returns(Result.Ok());
        _drugRepo.GetByIdAsync(inactiveDrug.Id, default).Returns(inactiveDrug);

        var result = await _sut.ReceiveAsync(request);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(ResultErrorCodes.Conflict);
        await _uow.DidNotReceive().SaveChangesAsync(default);
    }

    /// <summary>Branch not found → returns NotFound error without saving.</summary>
    [Fact]
    public async Task ReceiveAsync_WhenBranchNotFound_ReturnsNotFoundError()
    {
        var request = new ReceiveStockRequest(
            Guid.NewGuid(), ActiveDrug.Id, "LOT-005",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            10, 0, 5.00m, null);

        _validation.ValidateAsync(request, default).Returns(Result.Ok());
        _drugRepo.GetByIdAsync(ActiveDrug.Id, default).Returns(ActiveDrug);
        _branchRepo.GetByIdAsync(request.BranchId, default).Returns((Branch?)null);

        var result = await _sut.ReceiveAsync(request);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(ResultErrorCodes.NotFound);
        await _uow.DidNotReceive().SaveChangesAsync(default);
    }

    /// <summary>Validation fails → returns Validation error without saving.</summary>
    [Fact]
    public async Task ReceiveAsync_WhenValidationFails_ReturnsValidationError()
    {
        var request = new ReceiveStockRequest(
            Guid.Empty, Guid.Empty, string.Empty,
            DateOnly.MinValue, 0, 0, -1m, null);

        _validation.ValidateAsync(request, default)
            .Returns(Result.Fail(ResultError.Validation(
                new Dictionary<string, string[]> { ["Quantity"] = ["must be greater than 0."] })));

        var result = await _sut.ReceiveAsync(request);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(ResultErrorCodes.Validation);
        await _uow.DidNotReceive().SaveChangesAsync(default);
    }

    // --- AdjustAsync ---

    /// <summary>Valid adjustment → updates batch and returns DTO.</summary>
    [Fact]
    public async Task AdjustAsync_WhenValid_UpdatesBatchAndReturnsDto()
    {
        var batch = MakeBatch();
        var request = new AdjustStockRequest(80, 18.00m, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)), 0);

        _validation.ValidateAsync(request, default).Returns(Result.Ok());
        _repo.GetByIdAsync(batch.Id, default).Returns(batch);
        _repo.GetByIdWithDetailsAsync(batch.Id, default).Returns(batch);

        var result = await _sut.AdjustAsync(batch.Id, request);

        result.IsSuccess.ShouldBeTrue();
        await _uow.Received(1).SaveChangesAsync(default);
    }

    /// <summary>Batch not found → returns NotFound error.</summary>
    [Fact]
    public async Task AdjustAsync_WhenBatchNotFound_ReturnsNotFoundError()
    {
        var id = Guid.NewGuid();
        var request = new AdjustStockRequest(80, 18.00m, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)), 0);

        _validation.ValidateAsync(request, default).Returns(Result.Ok());
        _repo.GetByIdAsync(id, default).Returns((StockBatch?)null);

        var result = await _sut.AdjustAsync(id, request);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(ResultErrorCodes.NotFound);
        await _uow.DidNotReceive().SaveChangesAsync(default);
    }

    // --- DeleteAsync ---

    /// <summary>Batch exists → soft-deletes and returns Ok.</summary>
    [Fact]
    public async Task DeleteAsync_WhenBatchExists_SoftDeletesAndReturnsOk()
    {
        var batch = MakeBatch();
        _repo.GetByIdAsync(batch.Id, default).Returns(batch);

        var result = await _sut.DeleteAsync(batch.Id);

        result.IsSuccess.ShouldBeTrue();
        _repo.Received(1).Delete(batch);
        await _uow.Received(1).SaveChangesAsync(default);
    }

    /// <summary>Batch not found → returns NotFound error.</summary>
    [Fact]
    public async Task DeleteAsync_WhenBatchNotFound_ReturnsNotFoundError()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, default).Returns((StockBatch?)null);

        var result = await _sut.DeleteAsync(id);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(ResultErrorCodes.NotFound);
        await _uow.DidNotReceive().SaveChangesAsync(default);
    }

    // --- GetPagedAsync ---

    /// <summary>SearchAsync returns items → returns paginated list.</summary>
    [Fact]
    public async Task GetPagedAsync_ReturnsPaginatedList()
    {
        var batch = MakeBatch();
        _repo.SearchAsync(null, null, null, null, 0, 20, default)
            .Returns((new List<StockBatch> { batch }.AsReadOnly() as IReadOnlyList<StockBatch>, 1));

        var result = await _sut.GetPagedAsync(null, null, null, null, new PaginationParams());

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(1);
        result.Value.Items.Count.ShouldBe(1);
    }

    // --- Helpers ---

    private static StockBatch MakeBatch() =>
        StockBatch.Create(
            TestBranch.Id,
            ActiveDrug.Id,
            "LOT-001",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            100,
            0,
            10.00m,
            15.00m,
            DateTime.UtcNow);
}
