namespace Teryaq.Application.Features.Branches.Validators;

using FluentValidation;
using Teryaq.Application.Features.Branches.Dtos;

/// <summary>Validates <see cref="UpdateBranchRequest"/> before it reaches the service layer.</summary>
public sealed class UpdateBranchRequestValidator : AbstractValidator<UpdateBranchRequest>
{
    /// <summary>Initialises a new instance of <see cref="UpdateBranchRequestValidator"/>.</summary>
    public UpdateBranchRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Branch name is required.")
            .MaximumLength(200).WithMessage("Branch name must not exceed 200 characters.");

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters.")
            .When(x => x.Address is not null);

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Phone must not exceed 20 characters.")
            .When(x => x.Phone is not null);
    }
}
