namespace Teryaq.UnitTests.Features.Alerts;

using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Teryaq.Application.Common.Settings;
using Teryaq.Application.Features.Alerts;
using Teryaq.Application.Features.Alerts.Profiles;
using Teryaq.Domain.Features.Alerts;
using Teryaq.Domain.Features.Branches;
using Teryaq.Domain.Features.Drugs;
using Teryaq.Domain.Features.Inventory;
using Teryaq.Domain.Interfaces;
using Xunit;

/// <summary>Unit tests for <see cref="AlertService"/>.</summary>
public sealed class AlertServiceTests
{
    private readonly IStockBatchRepository _repo = Substitute.For<IStockBatchRepository>();
    private readonly IDateTime _dateTime = Substitute.For<IDateTime>();
    private readonly IMapper _mapper;
    private readonly AlertService _sut;

    private static readonly DateTime FakeNow = new(2026, 6, 29, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly FakeToday = DateOnly.FromDateTime(FakeNow);

    private static readonly Drug TestDrug = Drug.Create(
        "باراسيتامول", "Paracetamol", "Paracetamol", "Tablet", "500mg",
        20, 15.00m, null, null, null, DrugSource.Manual);

    private static readonly Branch TestBranch =
        Branch.Create(Guid.NewGuid(), "Main Branch", "123 Cairo St", "010-0000001");

    /// <summary>Initialises a new instance of <see cref="AlertServiceTests"/>.</summary>
    public AlertServiceTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<AlertProfile>(), NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _dateTime.UtcNow.Returns(FakeNow);

        var settings = Options.Create(new AlertSettings { NearExpiryDays = 90 });
        _sut = new AlertService(_repo, _mapper, _dateTime, settings);
    }

    // --- NearExpiry alerts ---

    /// <summary>Batches expiring within the window are returned as NearExpiry alerts with correct DaysUntilExpiry.</summary>
    [Fact]
    public async Task GetAlertsAsync_NearExpiryBatch_ReturnsNearExpiryAlertWithCorrectDays()
    {
        int expiresInDays = 30;
        var batch = MakeBatch(
            expiryDate: FakeToday.AddDays(expiresInDays),
            quantityOnHand: 10,
            reorderLevel: 0);

        var threshold = FakeToday.AddDays(90);
        _repo.GetNearExpiryAsync(null, threshold, default)
            .Returns(new List<StockBatch> { batch }.AsReadOnly() as IReadOnlyList<StockBatch>);
        _repo.GetLowStockAsync(null, default)
            .Returns(new List<StockBatch>().AsReadOnly() as IReadOnlyList<StockBatch>);

        var result = await _sut.GetAlertsAsync(null, null);

        result.IsSuccess.ShouldBeTrue();
        var alerts = result.Value;
        alerts.Count.ShouldBe(1);
        alerts[0].Type.ShouldBe(AlertType.NearExpiry);
        alerts[0].Severity.ShouldBe(AlertSeverity.High); // expiresInDays=30 ≤ NearExpiryHighDays(30)
        alerts[0].DaysUntilExpiry.ShouldBe(expiresInDays);
        alerts[0].StockBatchId.ShouldBe(batch.Id);
    }

    /// <summary>When type filter is NearExpiry, only near-expiry repo method is called.</summary>
    [Fact]
    public async Task GetAlertsAsync_WhenTypeIsNearExpiry_OnlyQueriesNearExpiry()
    {
        var threshold = FakeToday.AddDays(90);
        _repo.GetNearExpiryAsync(null, threshold, default)
            .Returns(new List<StockBatch>().AsReadOnly() as IReadOnlyList<StockBatch>);

        await _sut.GetAlertsAsync(null, AlertType.NearExpiry);

        await _repo.Received(1).GetNearExpiryAsync(Arg.Any<Guid?>(), Arg.Any<DateOnly>(), default);
        await _repo.DidNotReceive().GetLowStockAsync(Arg.Any<Guid?>(), default);
    }

    // --- LowStock alerts ---

    /// <summary>Batches at or below their reorder level are returned as LowStock alerts with null DaysUntilExpiry.</summary>
    [Fact]
    public async Task GetAlertsAsync_LowStockBatch_ReturnsLowStockAlertWithNullDaysUntilExpiry()
    {
        var batch = MakeBatch(
            expiryDate: FakeToday.AddDays(200),
            quantityOnHand: 5,
            reorderLevel: 10);

        var threshold = FakeToday.AddDays(90);
        _repo.GetNearExpiryAsync(null, threshold, default)
            .Returns(new List<StockBatch>().AsReadOnly() as IReadOnlyList<StockBatch>);
        _repo.GetLowStockAsync(null, default)
            .Returns(new List<StockBatch> { batch }.AsReadOnly() as IReadOnlyList<StockBatch>);

        var result = await _sut.GetAlertsAsync(null, null);

        result.IsSuccess.ShouldBeTrue();
        var alerts = result.Value;
        alerts.Count.ShouldBe(1);
        alerts[0].Type.ShouldBe(AlertType.LowStock);
        alerts[0].Severity.ShouldBe(AlertSeverity.Medium); // 5/10 = 0.50 ≤ LowStockMediumRatio(0.50)
        alerts[0].DaysUntilExpiry.ShouldBeNull();
        alerts[0].QuantityOnHand.ShouldBe(5);
        alerts[0].ReorderLevel.ShouldBe(10);
    }

    /// <summary>When type filter is LowStock, only low-stock repo method is called.</summary>
    [Fact]
    public async Task GetAlertsAsync_WhenTypeIsLowStock_OnlyQueriesLowStock()
    {
        _repo.GetLowStockAsync(null, default)
            .Returns(new List<StockBatch>().AsReadOnly() as IReadOnlyList<StockBatch>);

        await _sut.GetAlertsAsync(null, AlertType.LowStock);

        await _repo.DidNotReceive().GetNearExpiryAsync(Arg.Any<Guid?>(), Arg.Any<DateOnly>(), default);
        await _repo.Received(1).GetLowStockAsync(Arg.Any<Guid?>(), default);
    }

    // --- Combined and empty cases ---

    /// <summary>When both alert types are present, both are returned.</summary>
    [Fact]
    public async Task GetAlertsAsync_BothTypes_ReturnsBothAlerts()
    {
        var nearExpiry = MakeBatch(FakeToday.AddDays(10), 20, 0);
        var lowStock = MakeBatch(FakeToday.AddDays(300), 3, 15);

        var threshold = FakeToday.AddDays(90);
        _repo.GetNearExpiryAsync(null, threshold, default)
            .Returns(new List<StockBatch> { nearExpiry }.AsReadOnly() as IReadOnlyList<StockBatch>);
        _repo.GetLowStockAsync(null, default)
            .Returns(new List<StockBatch> { lowStock }.AsReadOnly() as IReadOnlyList<StockBatch>);

        var result = await _sut.GetAlertsAsync(null, null);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value.ShouldContain(a => a.Type == AlertType.NearExpiry);
        result.Value.ShouldContain(a => a.Type == AlertType.LowStock);
    }

    /// <summary>When the repo returns nothing, the result is an empty list — not an error.</summary>
    [Fact]
    public async Task GetAlertsAsync_EmptyRepo_ReturnsEmptyList()
    {
        var threshold = FakeToday.AddDays(90);
        _repo.GetNearExpiryAsync(null, threshold, default)
            .Returns(new List<StockBatch>().AsReadOnly() as IReadOnlyList<StockBatch>);
        _repo.GetLowStockAsync(null, default)
            .Returns(new List<StockBatch>().AsReadOnly() as IReadOnlyList<StockBatch>);

        var result = await _sut.GetAlertsAsync(null, null);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    /// <summary>Branch filter is forwarded to both repository calls.</summary>
    [Fact]
    public async Task GetAlertsAsync_WithBranchFilter_ForwardsBranchIdToBothRepos()
    {
        var branchId = Guid.NewGuid();
        var threshold = FakeToday.AddDays(90);
        _repo.GetNearExpiryAsync(branchId, threshold, default)
            .Returns(new List<StockBatch>().AsReadOnly() as IReadOnlyList<StockBatch>);
        _repo.GetLowStockAsync(branchId, default)
            .Returns(new List<StockBatch>().AsReadOnly() as IReadOnlyList<StockBatch>);

        await _sut.GetAlertsAsync(branchId, null);

        await _repo.Received(1).GetNearExpiryAsync(branchId, Arg.Any<DateOnly>(), default);
        await _repo.Received(1).GetLowStockAsync(branchId, default);
    }

    // --- Severity band boundary tests ---

    /// <summary>Near-expiry severity: ≤30 days → High, 31–60 → Medium, 61–90 → Low.</summary>
    [Theory]
    [InlineData(1, "High")]
    [InlineData(30, "High")]
    [InlineData(31, "Medium")]
    [InlineData(60, "Medium")]
    [InlineData(61, "Low")]
    [InlineData(90, "Low")]
    public async Task GetAlertsAsync_NearExpiryBand_ReturnsCorrectSeverity(int daysUntilExpiry, string expectedSeverity)
    {
        var batch = MakeBatch(FakeToday.AddDays(daysUntilExpiry), quantityOnHand: 10, reorderLevel: 0);
        var threshold = FakeToday.AddDays(90);
        _repo.GetNearExpiryAsync(null, threshold, default)
            .Returns(new List<StockBatch> { batch }.AsReadOnly() as IReadOnlyList<StockBatch>);
        _repo.GetLowStockAsync(null, default)
            .Returns(new List<StockBatch>().AsReadOnly() as IReadOnlyList<StockBatch>);

        var result = await _sut.GetAlertsAsync(null, AlertType.NearExpiry);

        result.IsSuccess.ShouldBeTrue();
        result.Value[0].Severity.ToString().ShouldBe(expectedSeverity);
    }

    /// <summary>Low-stock severity: ratio ≤0.25 → High, ≤0.50 → Medium, ≤1.00 → Low.</summary>
    [Theory]
    [InlineData(2, 10, "High")]   // 0.20 ≤ 0.25
    [InlineData(4, 10, "Medium")] // 0.40 ≤ 0.50
    [InlineData(5, 10, "Medium")] // 0.50 ≤ 0.50
    [InlineData(8, 10, "Low")]    // 0.80 > 0.50
    public async Task GetAlertsAsync_LowStockBand_ReturnsCorrectSeverity(int qty, int reorder, string expectedSeverity)
    {
        var batch = MakeBatch(FakeToday.AddDays(200), quantityOnHand: qty, reorderLevel: reorder);
        var threshold = FakeToday.AddDays(90);
        _repo.GetNearExpiryAsync(null, threshold, default)
            .Returns(new List<StockBatch>().AsReadOnly() as IReadOnlyList<StockBatch>);
        _repo.GetLowStockAsync(null, default)
            .Returns(new List<StockBatch> { batch }.AsReadOnly() as IReadOnlyList<StockBatch>);

        var result = await _sut.GetAlertsAsync(null, AlertType.LowStock);

        result.IsSuccess.ShouldBeTrue();
        result.Value[0].Severity.ToString().ShouldBe(expectedSeverity);
    }

    // --- Helpers ---

    private static StockBatch MakeBatch(DateOnly expiryDate, int quantityOnHand, int reorderLevel)
    {
        var batch = StockBatch.Create(
            TestBranch.Id,
            TestDrug.Id,
            "LOT-001",
            expiryDate,
            quantityOnHand,
            reorderLevel,
            10.00m,
            15.00m,
            DateTime.UtcNow);

        // Wire navigation properties via reflection so the mapper can flatten them.
        typeof(StockBatch)
            .GetProperty(nameof(StockBatch.Drug))!
            .SetValue(batch, TestDrug);
        typeof(StockBatch)
            .GetProperty(nameof(StockBatch.Branch))!
            .SetValue(batch, TestBranch);

        return batch;
    }
}
