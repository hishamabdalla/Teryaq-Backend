namespace Teryaq.API.Controllers;

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Teryaq.API.Controllers.Base;
using Teryaq.Application.Features.Alerts;
using Teryaq.Application.Features.Alerts.Dtos;
using Teryaq.Domain.Features.Alerts;

/// <summary>Provides on-demand near-expiry and low-stock alerts for the current tenant's inventory.</summary>
[ApiVersion(1)]
[Authorize(Policy = "PharmacyStaff")]
public sealed class AlertsController : ApiControllerBase
{
    private readonly IAlertService _alertService;

    /// <summary>Initialises a new instance of <see cref="AlertsController"/>.</summary>
    public AlertsController(IAlertService alertService)
    {
        _alertService = alertService;
    }

    /// <summary>Returns all active near-expiry and low-stock alerts for the current tenant.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AlertDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] Guid? branchId,
        [FromQuery] AlertType? type,
        CancellationToken ct)
    {
        var result = await _alertService.GetAlertsAsync(branchId, type, ct);
        return HandleResult(result);
    }
}
