namespace Teryaq.Domain.Features.Products;

using Teryaq.Domain.Interfaces;

/// <summary>Data-access contract for the <see cref="Product"/> aggregate.</summary>
public interface IProductRepository : IRepository<Product>
{
}
