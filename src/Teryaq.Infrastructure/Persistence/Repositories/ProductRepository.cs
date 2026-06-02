namespace Teryaq.Infrastructure.Persistence.Repositories;

using Teryaq.Domain.Features.Products;

/// <inheritdoc cref="IProductRepository"/>
public sealed class ProductRepository : GenericRepository<Product>, IProductRepository
{
    /// <summary>Initialises a new instance of <see cref="ProductRepository"/>.</summary>
    public ProductRepository(AppDbContext context) : base(context) { }
}
