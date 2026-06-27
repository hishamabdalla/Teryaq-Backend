namespace Teryaq.Application.Features.Inventory.Validators;

using FluentValidation;
using Teryaq.Application.Features.Inventory.Dtos;

/// <summary>Validates <see cref="ReceiveStockRequest"/> before it reaches the service layer.</summary>
public sealed class ReceiveStockRequestValidator : AbstractValidator<ReceiveStockRequest>
{
    /// <summary>Initialises a new instance of <see cref="ReceiveStockRequestValidator"/>.</summary>
    public ReceiveStockRequestValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.DrugId).NotEmpty();
        RuleFor(x => x.BatchNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ExpiryDate)
            .Must(date => date > DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Expiry date must be a future date.");
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.CostPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SellingPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.SellingPrice.HasValue);
    }
}
