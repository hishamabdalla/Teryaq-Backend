namespace Teryaq.UnitTests.Features.Drugs;

using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Teryaq.Application.Common;
using Teryaq.Application.Common.Validation;
using Teryaq.Application.Features.Drugs;
using Teryaq.Application.Features.Drugs.Dtos;
using Teryaq.Application.Features.Drugs.Profiles;
using Teryaq.Domain.Features.Drugs;
using Teryaq.Domain.Interfaces;
using Xunit;

public sealed class DrugServiceTests
{
    private readonly IDrugRepository _repo = Substitute.For<IDrugRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IValidationService _validation = Substitute.For<IValidationService>();
    private readonly IMapper _mapper;
    private readonly DrugService _sut;

    public DrugServiceTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DrugProfile>(), NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _sut = new DrugService(_repo, _uow, _validation, _mapper);
    }

    // --- GetByIdAsync ---

    [Fact]
    public async Task GetByIdAsync_WhenDrugExists_ReturnsDto()
    {
        var drug = Drug.Create("باراسيتامول", "Paracetamol", "Paracetamol", "Tablet",
            "500mg", 20, 15.00m, null, null, null, DrugSource.Manual);

        _repo.GetByIdAsync(drug.Id, default).Returns(drug);

        var result = await _sut.GetByIdAsync(drug.Id);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TradeNameEn.ShouldBe("Paracetamol");
        result.Value.Id.ShouldBe(drug.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenDrugNotFound_ReturnsNotFoundError()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, default).Returns((Drug?)null);

        var result = await _sut.GetByIdAsync(id);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(ResultErrorCodes.NotFound);
    }

    // --- CreateAsync ---

    [Fact]
    public async Task CreateAsync_WhenValid_CreatesDrugAndReturnsDto()
    {
        var request = new CreateDrugRequest(
            "باراسيتامول", "Paracetamol", "Paracetamol",
            "Tablet", "500mg", 20, 15.00m,
            null, null, null, DrugSource.Manual);

        _validation.ValidateAsync(request, default).Returns(Result.Ok());
        _repo.ExistsByNameAndFormAsync("Paracetamol", "500mg", "Tablet", null, default).Returns(false);

        var result = await _sut.CreateAsync(request);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TradeNameEn.ShouldBe("Paracetamol");
        await _uow.Received(1).SaveChangesAsync(default);
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicate_ReturnsConflictError()
    {
        var request = new CreateDrugRequest(
            "باراسيتامول", "Paracetamol", "Paracetamol",
            "Tablet", "500mg", 20, 15.00m,
            null, null, null, DrugSource.Manual);

        _validation.ValidateAsync(request, default).Returns(Result.Ok());
        _repo.ExistsByNameAndFormAsync("Paracetamol", "500mg", "Tablet", null, default).Returns(true);

        var result = await _sut.CreateAsync(request);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(ResultErrorCodes.Conflict);
        await _uow.DidNotReceive().SaveChangesAsync(default);
    }

    [Fact]
    public async Task CreateAsync_WhenValidationFails_ReturnsValidationError()
    {
        var request = new CreateDrugRequest(
            "", "", "", "", "", 0, -1m, null, null, null, DrugSource.Manual);

        _validation.ValidateAsync(request, default)
            .Returns(Result.Fail(ResultError.Validation(new Dictionary<string, string[]> { ["TradeNameEn"] = ["must not be empty."] })));

        var result = await _sut.CreateAsync(request);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(ResultErrorCodes.Validation);
        await _uow.DidNotReceive().SaveChangesAsync(default);
    }

    // --- UpdateAsync ---

    [Fact]
    public async Task UpdateAsync_WhenDrugNotFound_ReturnsNotFoundError()
    {
        var id = Guid.NewGuid();
        var request = new UpdateDrugRequest(
            "باراسيتامول", "Paracetamol", "Paracetamol",
            "Tablet", "500mg", 20, 15.00m,
            null, null, null, true);

        _validation.ValidateAsync(request, default).Returns(Result.Ok());
        _repo.GetByIdAsync(id, default).Returns((Drug?)null);

        var result = await _sut.UpdateAsync(id, request);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(ResultErrorCodes.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_WhenDuplicateNameOnOtherDrug_ReturnsConflictError()
    {
        var drug = Drug.Create("باراسيتامول", "Paracetamol", "Paracetamol", "Tablet",
            "500mg", 20, 15.00m, null, null, null, DrugSource.Manual);

        var request = new UpdateDrugRequest(
            "باراسيتامول", "Paracetamol", "Paracetamol",
            "Tablet", "500mg", 20, 15.00m,
            null, null, null, true);

        _validation.ValidateAsync(request, default).Returns(Result.Ok());
        _repo.GetByIdAsync(drug.Id, default).Returns(drug);
        _repo.ExistsByNameAndFormAsync("Paracetamol", "500mg", "Tablet", drug.Id, default).Returns(true);

        var result = await _sut.UpdateAsync(drug.Id, request);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(ResultErrorCodes.Conflict);
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_WhenDrugExists_SoftDeletesAndReturnsOk()
    {
        var drug = Drug.Create("باراسيتامول", "Paracetamol", "Paracetamol", "Tablet",
            "500mg", 20, 15.00m, null, null, null, DrugSource.Manual);

        _repo.GetByIdAsync(drug.Id, default).Returns(drug);

        var result = await _sut.DeleteAsync(drug.Id);

        result.IsSuccess.ShouldBeTrue();
        _repo.Received(1).Delete(drug);
        await _uow.Received(1).SaveChangesAsync(default);
    }

    [Fact]
    public async Task DeleteAsync_WhenDrugNotFound_ReturnsNotFoundError()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, default).Returns((Drug?)null);

        var result = await _sut.DeleteAsync(id);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(ResultErrorCodes.NotFound);
        await _uow.DidNotReceive().SaveChangesAsync(default);
    }
}
