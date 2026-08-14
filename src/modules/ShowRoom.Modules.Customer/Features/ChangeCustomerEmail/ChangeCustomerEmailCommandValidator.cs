using FluentValidation;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.SharedKernel.Emails;

namespace ShowRoom.Modules.Customer.Features.ChangeCustomerEmail;

internal sealed class ChangeCustomerEmailCommandValidator : AbstractValidator<ChangeCustomerEmailCommand>
{
    public ChangeCustomerEmailCommandValidator()
    {
        RuleFor(x => x.PublicId)
            .NotEmpty().WithMessage("Customer public id is required.")
            .Must(PublicId.IsValid).WithMessage("Customer public id is not a valid public id.");

        RuleFor(x => x.NewEmail)
            .NotEmpty().WithMessage("Email is required.")
            .Must(Email.IsValid).WithMessage("Email is invalid.");
    }
}
