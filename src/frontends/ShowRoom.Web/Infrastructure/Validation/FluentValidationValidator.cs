using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace ShowRoom.Web.Infrastructure.Validation;

/// <summary>
/// Plugs FluentValidation into the ambient <see cref="EditContext"/>. Drop
/// <c>&lt;FluentValidationValidator /&gt;</c> inside an <c>&lt;EditForm&gt;</c>; the validator for the
/// form model is resolved from DI.
/// </summary>
/// <remarks>
/// On submit the whole model is validated. On a single field change only that field's messages are
/// replaced, so touching one input never surfaces errors on inputs the user has not filled yet — and
/// never duplicates the messages already stored for the other fields.
/// </remarks>
public sealed class FluentValidationValidator : ComponentBase, IDisposable
{
    private ValidationMessageStore messageStore = default!;

    [CascadingParameter]
    private EditContext EditContext { get; set; } = default!;

    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = default!;

    protected override void OnInitialized()
    {
        ArgumentNullException.ThrowIfNull(EditContext);

        messageStore = new ValidationMessageStore(EditContext);

        EditContext.OnValidationRequested += OnValidationRequested;
        EditContext.OnFieldChanged += OnFieldChanged;
    }

    public void Dispose()
    {
        if (EditContext is not null)
        {
            EditContext.OnValidationRequested -= OnValidationRequested;
            EditContext.OnFieldChanged -= OnFieldChanged;
        }
    }

    private void OnValidationRequested(object? sender, ValidationRequestedEventArgs args)
    {
        messageStore.Clear();

        foreach (var error in Validate())
        {
            messageStore.Add(EditContext.Field(error.PropertyName), error.ErrorMessage);
        }

        EditContext.NotifyValidationStateChanged();
    }

    private void OnFieldChanged(object? sender, FieldChangedEventArgs args)
    {
        var field = args.FieldIdentifier;
        messageStore.Clear(field);

        foreach (var error in Validate().Where(x => x.PropertyName == field.FieldName))
        {
            messageStore.Add(field, error.ErrorMessage);
        }

        EditContext.NotifyValidationStateChanged();
    }

    private IEnumerable<FluentValidation.Results.ValidationFailure> Validate()
    {
        var model = EditContext.Model;
        var validatorType = typeof(IValidator<>).MakeGenericType(model.GetType());

        if (ServiceProvider.GetService(validatorType) is not IValidator validator)
        {
            return [];
        }

        return validator.Validate(new ValidationContext<object>(model)).Errors;
    }
}
