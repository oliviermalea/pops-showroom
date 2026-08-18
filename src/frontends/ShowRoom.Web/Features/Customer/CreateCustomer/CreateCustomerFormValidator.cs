using FluentValidation;

namespace ShowRoom.Web.Features.Customer.CreateCustomer;

/// <summary>
/// Client-side rules, mirroring the backend's <c>CreateCustomerCommandValidator</c> constraints so the
/// user gets immediate feedback. The backend stays the authority: a rule that passes here can still be
/// rejected server-side (a duplicate email, for instance).
/// </summary>
public sealed class CreateCustomerFormValidator : AbstractValidator<CreateCustomerForm>
{
    public CreateCustomerFormValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Le prénom est obligatoire.")
            .MaximumLength(200).WithMessage("Le prénom ne doit pas dépasser 200 caractères.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Le nom est obligatoire.")
            .MaximumLength(200).WithMessage("Le nom ne doit pas dépasser 200 caractères.");

        // Stop at the first failing rule: an empty email must read "obligatoire", not also "invalide".
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("L'email est obligatoire.")
            .MaximumLength(320).WithMessage("L'email ne doit pas dépasser 320 caractères.")
            .EmailAddress().WithMessage("Saisissez une adresse email valide.");

        RuleFor(x => x.Phone)
            .MaximumLength(40).WithMessage("Le téléphone ne doit pas dépasser 40 caractères.")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}
