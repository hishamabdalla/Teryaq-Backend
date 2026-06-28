namespace Teryaq.Application.Features.Alerts;

using AutoMapper;
using Microsoft.Extensions.Options;
using Teryaq.Application.Common;
using Teryaq.Application.Common.Settings;
using Teryaq.Application.Features.Alerts.Dtos;
using Teryaq.Domain.Features.Alerts;
using Teryaq.Domain.Features.Inventory;
using Teryaq.Domain.Interfaces;
using static Teryaq.Domain.Features.Alerts.AlertSeverity;

/// <inheritdoc cref="IAlertService"/>
public sealed class AlertService : IAlertService
{
    private readonly IStockBatchRepository _stockBatchRepository;
    private readonly IMapper _mapper;
    private readonly IDateTime _dateTime;
    private readonly AlertSettings _settings;

    /// <summary>Initialises a new instance of <see cref="AlertService"/>.</summary>
    public AlertService(
        IStockBatchRepository stockBatchRepository,
        IMapper mapper,
        IDateTime dateTime,
        IOptions<AlertSettings> settings)
    {
        _stockBatchRepository = stockBatchRepository;
        _mapper = mapper;
        _dateTime = dateTime;
        _settings = settings.Value;
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<AlertDto>>> GetAlertsAsync(
        Guid? branchId,
        AlertType? type,
        CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(_dateTime.UtcNow);
        var alerts = new List<AlertDto>();

        if (type is null or AlertType.NearExpiry)
        {
            var threshold = today.AddDays(_settings.NearExpiryDays);
            var batches = await _stockBatchRepository.GetNearExpiryAsync(branchId, threshold, ct);

            foreach (var batch in batches)
            {
                int daysUntilExpiry = batch.ExpiryDate.DayNumber - today.DayNumber;
                var dto = _mapper.Map<AlertDto>(batch) with
                {
                    Type = AlertType.NearExpiry,
                    Severity = NearExpirySeverity(daysUntilExpiry),
                    DaysUntilExpiry = daysUntilExpiry,
                };
                alerts.Add(dto);
            }
        }

        if (type is null or AlertType.LowStock)
        {
            var batches = await _stockBatchRepository.GetLowStockAsync(branchId, ct);

            foreach (var batch in batches)
            {
                var dto = _mapper.Map<AlertDto>(batch) with
                {
                    Type = AlertType.LowStock,
                    Severity = LowStockSeverity(batch.QuantityOnHand, batch.ReorderLevel),
                    DaysUntilExpiry = null,
                };
                alerts.Add(dto);
            }
        }

        return Result.Ok<IReadOnlyList<AlertDto>>(alerts);
    }

    private AlertSeverity NearExpirySeverity(int days) =>
        days <= _settings.NearExpiryHighDays ? High :
        days <= _settings.NearExpiryMediumDays ? Medium :
        Low;

    private AlertSeverity LowStockSeverity(int qty, int reorder)
    {
        if (reorder <= 0)
            return Low;

        double ratio = (double)qty / reorder;
        return ratio <= _settings.LowStockHighRatio ? High :
               ratio <= _settings.LowStockMediumRatio ? Medium :
               Low;
    }
}
