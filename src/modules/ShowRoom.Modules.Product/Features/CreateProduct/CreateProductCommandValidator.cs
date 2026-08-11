using FluentValidation;
using ShowRoom.SharedKernel.Currencies;

namespace ShowRoom.Modules.Product.Features.CreateProduct;

internal sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(300).WithMessage("Name must not exceed 300 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price must be non-negative.");

        RuleFor(x => x.Currency)
            .Must(Currency.IsValidCode)
            .WithMessage("Currency must be a supported 3-letter uppercase ISO 4217 code (e.g. \"EUR\").")
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));
    }
}
