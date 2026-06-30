namespace Teryaq.Infrastructure.Persistence.Repositories;

using Teryaq.Domain.Features.Inventory;

/// <summary>EF Core implementation of <see cref="IStockMovementRepository"/>.</summary>
public sealed class StockMovementRepository : GenericRepository<StockMovement>, IStockMovementRepository
{
    /// <summary>Initialises a new instance of <see cref="StockMovementRepository"/>.</summary>
    public StockMovementRepository(AppDbContext context) : base(context) { }
}
