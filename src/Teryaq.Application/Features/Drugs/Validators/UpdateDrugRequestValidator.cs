namespace Teryaq.Application.Features.Drugs.Validators;

using FluentValidation;
using Teryaq.Application.Features.Drugs.Dtos;

/// <summary>Validates <see cref="UpdateDrugRequest"/> before it reaches the service layer.</summary>
public sealed class UpdateDrugRequestValidator : AbstractValidator<UpdateDrugRequest>
{
    /// <summary>Initialises a new instance of <see cref="UpdateDrugRequestValidator"/>.</summary>
    public UpdateDrugRequestValidator()
    {
        RuleFor(x => x.TradeNameAr).NotEmpty().MaximumLength(300);
        RuleFor(x => x.TradeNameEn).NotEmpty().MaximumLength(300);
        RuleFor(x => x.GenericName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.DosageForm).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Strength).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PackSize).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Barcode).MaximumLength(50).When(x => x.Barcode is not null);
        RuleFor(x => x.ManufacturerAr).MaximumLength(300).When(x => x.ManufacturerAr is not null);
        RuleFor(x => x.ManufacturerEn).MaximumLength(300).When(x => x.ManufacturerEn is not null);
    }
}
