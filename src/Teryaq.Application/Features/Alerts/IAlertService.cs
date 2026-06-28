namespace Teryaq.Application.Features.Alerts;

using Teryaq.Application.Common;
using Teryaq.Application.Features.Alerts.Dtos;
using Teryaq.Domain.Features.Alerts;

/// <summary>Provides on-demand near-expiry and low-stock alert queries for the current tenant.</summary>
public interface IAlertService
{
    /// <summary>
    /// Returns all active alerts for the current tenant, optionally filtered by branch and alert type.
    /// Near-expiry alerts surface batches expiring within the configured window; low-stock alerts
    /// surface batches whose on-hand quantity is at or below their reorder level.
    /// </summary>
    /// <param name="branchId">Optional branch filter; when <see langword="null"/> all branches are included.</param>
    /// <param name="type">Optional alert type filter; when <see langword="null"/> both types are returned.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<IReadOnlyList<AlertDto>>> GetAlertsAsync(Guid? branchId, AlertType? type, CancellationToken ct = default);
}
