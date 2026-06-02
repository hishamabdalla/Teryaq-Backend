namespace Teryaq.Application.Features.Products;

using AutoMapper;
using Microsoft.Extensions.Logging;
using Teryaq.Application.Common;
using Teryaq.Application.Common.Pagination;
using Teryaq.Application.Common.Validation;
using Teryaq.Application.Features.Products.Dtos;
using Teryaq.Domain.Features.Products;
using Teryaq.Domain.Interfaces;

/// <inheritdoc cref="IProductService"/>
public sealed partial class ProductService : IProductService
{
    private readonly IRepository<Product> _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IValidationService _validationService;
    private readonly ILogger<ProductService> _logger;

    /// <summary>Initialises a new instance of <see cref="ProductService"/>.</summary>
    public ProductService(
        IRepository<Product> productRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IValidationService validationService,
        ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _validationService = validationService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result<ProductDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _productRepository.GetByIdAsync(id, ct);
        if (product is null)
        {
            Log.ProductNotFound(_logger, id);
            return ResultError.NotFound<Product>(id);
        }

        return _mapper.Map<ProductDto>(product);
    }

    /// <inheritdoc/>
    public async Task<Result<PaginatedList<ProductDto>>> GetAllAsync(PaginationParams pagination, CancellationToken ct = default)
    {
        int total = await _productRepository.CountAsync(ct);
        var items = await _productRepository.GetPagedAsync(pagination.Skip, pagination.PageSize, ct);

        var dtos = _mapper.Map<IReadOnlyList<ProductDto>>(items);
        return PaginatedList<ProductDto>.Create(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    /// <inheritdoc/>
    public async Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var validation = await _validationService.ValidateAsync(request, ct);
        if (validation.IsFailure)
        {
            Log.CreateValidationFailed(_logger, validation.Error.Message);
            return validation.Error;
        }

        var product = Product.Create(request.Name, request.Description, request.Price);
        await _productRepository.AddAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        Log.ProductCreated(_logger, product.Id);
        return _mapper.Map<ProductDto>(product);
    }

    /// <inheritdoc/>
    public async Task<Result<ProductDto>> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var validation = await _validationService.ValidateAsync(request, ct);
        if (validation.IsFailure)
        {
            Log.UpdateValidationFailed(_logger, id, validation.Error.Message);
            return validation.Error;
        }

        var product = await _productRepository.GetByIdAsync(id, ct);
        if (product is null)
        {
            Log.ProductNotFound(_logger, id);
            return ResultError.NotFound<Product>(id);
        }

        product.Update(request.Name, request.Description, request.Price);
        await _unitOfWork.SaveChangesAsync(ct);

        Log.ProductUpdated(_logger, id);
        return _mapper.Map<ProductDto>(product);
    }

    /// <inheritdoc/>
    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _productRepository.GetByIdAsync(id, ct);
        if (product is null)
        {
            Log.ProductNotFound(_logger, id);
            return Result.Fail(ResultError.NotFound<Product>(id));
        }

        _productRepository.Delete(product);
        await _unitOfWork.SaveChangesAsync(ct);

        Log.ProductDeleted(_logger, id);
        return Result.Ok();
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Warning, Message = "Product {ProductId} was not found.")]
        public static partial void ProductNotFound(ILogger logger, Guid productId);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Create product validation failed: {Errors}")]
        public static partial void CreateValidationFailed(ILogger logger, string errors);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Update product {ProductId} validation failed: {Errors}")]
        public static partial void UpdateValidationFailed(ILogger logger, Guid productId, string errors);

        [LoggerMessage(Level = LogLevel.Information, Message = "Product {ProductId} created.")]
        public static partial void ProductCreated(ILogger logger, Guid productId);

        [LoggerMessage(Level = LogLevel.Information, Message = "Product {ProductId} updated.")]
        public static partial void ProductUpdated(ILogger logger, Guid productId);

        [LoggerMessage(Level = LogLevel.Information, Message = "Product {ProductId} deleted.")]
        public static partial void ProductDeleted(ILogger logger, Guid productId);
    }
}
