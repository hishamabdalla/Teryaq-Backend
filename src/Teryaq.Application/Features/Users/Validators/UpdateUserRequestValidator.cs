namespace Teryaq.Application.Features.Users.Validators;

using FluentValidation;
using Teryaq.Application.Features.Users.Dtos;

/// <summary>Validates <see cref="UpdateUserRequest"/> before it reaches the service layer.</summary>
public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    /// <summary>Initialises a new instance of <see cref="UpdateUserRequestValidator"/>.</summary>
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(200).WithMessage("Full name must not exceed 200 characters.");
    }
}
