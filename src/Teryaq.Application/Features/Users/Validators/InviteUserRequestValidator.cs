namespace Teryaq.Application.Features.Users.Validators;

using FluentValidation;
using Teryaq.Application.Features.Users.Dtos;

/// <summary>Validates <see cref="InviteUserRequest"/> before it reaches the service layer.</summary>
public sealed class InviteUserRequestValidator : AbstractValidator<InviteUserRequest>
{
    /// <summary>Initialises a new instance of <see cref="InviteUserRequestValidator"/>.</summary>
    public InviteUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(200).WithMessage("Full name must not exceed 200 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");
    }
}
