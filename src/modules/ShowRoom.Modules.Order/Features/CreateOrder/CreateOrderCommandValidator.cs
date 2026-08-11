using FluentValidation;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.SharedKernel.Currencies;

namespace ShowRoom.Modules.Order.Features.CreateOrder;

internal sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerPublicId)
            .NotEmpty().WithMessage("Customer public id is required.")
            .Must(PublicId.IsValid).WithMessage("Customer public id is not a valid public id.");

        RuleFor(x => x.Currency)
            .Must(Currency.IsValidCode)
            .WithMessage("Currency must be a supported 3-letter uppercase ISO 4217 code (e.g. \"EUR\").")
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("An order must contain at least one line.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductPublicId)
                .NotEmpty().WithMessage("Product public id is required.")
                .Must(PublicId.IsValid).WithMessage("Product public id is not a valid public id.");

            line.RuleFor(l => l.ProductName)
                .NotEmpty().WithMessage("Product name is required.")
                .MaximumLength(300).WithMessage("Product name must not exceed 300 characters.");

            line.RuleFor(l => l.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be strictly positive.");

            line.RuleFor(l => l.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Unit price must be non-negative.");
        });
    }
}
