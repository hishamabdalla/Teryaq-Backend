namespace Teryaq.UnitTests.Features.Products;

using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Teryaq.Application.Common;
using Teryaq.Application.Common.Pagination;
using Teryaq.Application.Common.Validation;
using Teryaq.Application.Features.Products;
using Teryaq.Application.Features.Products.Dtos;
using Teryaq.Domain.Features.Products;
using Teryaq.Domain.Interfaces;
using Shouldly;
using Xunit;

public sealed class ProductServiceTests
{
    private readonly IRepository<Product> _repository = Substitute.For<IRepository<Product>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly IValidationService _validationService = Substitute.For<IValidationService>();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _sut = new ProductService(
            _repository,
            _unitOfWork,
            _mapper,
            _validationService,
            NullLogger<ProductService>.Instance);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsProductDto_WhenProductExists()
    {
        var product = Product.Create("Widget", "A widget", 9.99m);
        var dto = new ProductDto(product.Id, product.Name, product.Description, product.Price, DateTime.UtcNow, null);

        _repository.GetByIdAsync(product.Id).Returns(product);
        _mapper.Map<ProductDto>(product).Returns(dto);

        var result = await _sut.GetByIdAsync(product.Id);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(dto);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFound_WhenProductDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id).Returns((Product?)null);

        var result = await _sut.GetByIdAsync(id);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(ResultErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedList()
    {
        var pagination = new PaginationParams { PageNumber = 1, PageSize = 10 };
        var product = Product.Create("Widget", null, 1m);
        var dto = new ProductDto(product.Id, product.Name, product.Description, product.Price, DateTime.UtcNow, null);

        _repository.CountAsync().Returns(1);
        _repository.GetPagedAsync(0, 10).Returns([product]);
        _mapper.Map<IReadOnlyList<ProductDto>>(Arg.Any<IReadOnlyList<Product>>()).Returns([dto]);

        var result = await _sut.GetAllAsync(pagination);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(1);
        result.Value.Items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedProduct_WhenRequestIsValid()
    {
        var request = new CreateProductRequest("Widget", "A widget", 9.99m);
        var product = Product.Create(request.Name, request.Description, request.Price);
        var dto = new ProductDto(product.Id, product.Name, product.Description, product.Price, DateTime.UtcNow, null);

        _validationService.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(Result.Ok());
        _mapper.Map<ProductDto>(Arg.Any<Product>()).Returns(dto);

        var result = await _sut.CreateAsync(request);

        result.IsSuccess.ShouldBeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationFailure_WhenRequestIsInvalid()
    {
        var request = new CreateProductRequest(string.Empty, null, -1m);

        _validationService.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(Result.Fail(ResultError.Validation("Name is required.")));

        var result = await _sut.CreateAsync(request);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(ResultErrorCodes.Validation);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ReturnsUpdatedProduct_WhenProductExistsAndRequestIsValid()
    {
        var product = Product.Create("Old Name", null, 1m);
        var request = new UpdateProductRequest("New Name", "Desc", 2m);
        var dto = new ProductDto(product.Id, "New Name", "Desc", 2m, DateTime.UtcNow, DateTime.UtcNow);

        _validationService.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(Result.Ok());
        _repository.GetByIdAsync(product.Id).Returns(product);
        _mapper.Map<ProductDto>(product).Returns(dto);

        var result = await _sut.UpdateAsync(product.Id, request);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("New Name");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFound_WhenProductDoesNotExist()
    {
        var id = Guid.NewGuid();
        var request = new UpdateProductRequest("Name", null, 1m);

        _validationService.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(Result.Ok());
        _repository.GetByIdAsync(id).Returns((Product?)null);

        var result = await _sut.UpdateAsync(id, request);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(ResultErrorCodes.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsValidationFailure_WhenRequestIsInvalid()
    {
        var id = Guid.NewGuid();
        var request = new UpdateProductRequest(string.Empty, null, -1m);

        _validationService.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(Result.Fail(ResultError.Validation("Name is required.")));

        var result = await _sut.UpdateAsync(id, request);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(ResultErrorCodes.Validation);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsOk_WhenProductExists()
    {
        var product = Product.Create("Widget", null, 1m);
        _repository.GetByIdAsync(product.Id).Returns(product);

        var result = await _sut.DeleteAsync(product.Id);

        result.IsSuccess.ShouldBeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsNotFound_WhenProductDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id).Returns((Product?)null);

        var result = await _sut.DeleteAsync(id);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(ResultErrorCodes.NotFound);
    }
}
