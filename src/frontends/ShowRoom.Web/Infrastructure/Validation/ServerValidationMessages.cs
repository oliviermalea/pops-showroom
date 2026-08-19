using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using ShowRoom.Web.Infrastructure.Api.Problems;

namespace ShowRoom.Web.Infrastructure.Validation;

/// <summary>
/// Places the API's server-side verdict on the form fields it names. Drop
/// <c>&lt;ServerValidationMessages @ref="…" /&gt;</c> inside an <c>&lt;EditForm&gt;</c>, then call
/// <see cref="Show"/> with the <see cref="ApiProblem"/> returned by the facade.
/// </summary>
/// <remarks>
/// <para>It writes into its OWN <see cref="ValidationMessageStore"/>, next to the one
/// <see cref="FluentValidationValidator"/> fills client-side: the two verdicts have different lifetimes
/// and must not overwrite each other.</para>
///
/// <para>Clearing on every validation request is not cosmetic. <c>EditContext.Validate()</c> is false
/// while ANY store holds a message, so a server error left behind would veto every later submit — the
/// form would silently refuse to send. Clearing per field on edit is the same reasoning one level down:
/// the moment the user changes what the server rejected, the verdict no longer applies.</para>
///
/// <para>It deliberately does NOT take the problem as a parameter: a parent re-render would then replay
/// a stale verdict just after it was cleared. The page decides when a verdict starts to apply.</para>
/// </remarks>
public sealed class ServerValidationMessages : ComponentBase, IDisposable
{
    private EditContext? boundContext;
    private ValidationMessageStore? store;

    [CascadingParameter]
    private EditContext EditContext { get; set; } = default!;

    /// <summary>
    /// Shows a server verdict: the problem's field errors, plus any message the screen maps itself (a
    /// business code such as a uniqueness conflict names no field, but the screen knows which input it
    /// concerns). Returns whether at least one message could be placed on a field.
    /// </summary>
    /// <param name="problem">The ProblemDetails returned by the API, if any.</param>
    /// <param name="translate">
    /// Optional resolution of the displayed text from the field name and the server message.
    /// </param>
    /// <param name="extra">Messages the screen maps itself, as (field, message) pairs.</param>
    public bool Show(
        ApiProblem? problem,
        Func<string, string, string>? translate = null,
        IEnumerable<(string Field, string Message)>? extra = null)
    {
        EnsureBound();

        store!.Clear();
        var placed = false;

        if (problem is not null)
        {
            foreach (var (field, messages) in problem.FieldErrors)
            {
                if (!IsModelField(field))
                {
                    // A code naming something this form does not carry would sit in the store unseen —
                    // invisible to the user, yet enough to block the next submit. It stays in the
                    // diagnostic panel instead.
                    continue;
                }

                foreach (var message in messages)
                {
                    store.Add(EditContext.Field(field), translate?.Invoke(field, message) ?? message);
                    placed = true;
                }
            }
        }

        foreach (var (field, message) in extra ?? [])
        {
            if (IsModelField(field))
            {
                store.Add(EditContext.Field(field), message);
                placed = true;
            }
        }

        EditContext.NotifyValidationStateChanged();

        return placed;
    }

    /// <summary>Drops the current server verdict, without touching the client-side messages.</summary>
    public void Clear()
    {
        EnsureBound();
        store!.Clear();
        EditContext.NotifyValidationStateChanged();
    }

    protected override void OnInitialized() => EnsureBound();

    protected override void OnParametersSet() => EnsureBound();

    public void Dispose() => Unbind();

    /// <summary>
    /// Binds to the ambient <see cref="EditContext"/>, rebinding when the form swaps it (resetting the
    /// screen replaces the model, hence the context — a store bound to the old one would write into a
    /// form nobody is looking at).
    /// </summary>
    private void EnsureBound()
    {
        ArgumentNullException.ThrowIfNull(EditContext);

        if (ReferenceEquals(boundContext, EditContext))
        {
            return;
        }

        Unbind();

        boundContext = EditContext;
        store = new ValidationMessageStore(EditContext);

        EditContext.OnValidationRequested += OnValidationRequested;
        EditContext.OnFieldChanged += OnFieldChanged;
    }

    private void Unbind()
    {
        if (boundContext is null)
        {
            return;
        }

        boundContext.OnValidationRequested -= OnValidationRequested;
        boundContext.OnFieldChanged -= OnFieldChanged;
        boundContext = null;
        store = null;
    }

    private void OnValidationRequested(object? sender, ValidationRequestedEventArgs args) => store?.Clear();

    private void OnFieldChanged(object? sender, FieldChangedEventArgs args) => store?.Clear(args.FieldIdentifier);

    private bool IsModelField(string field) => EditContext.Model.GetType().GetProperty(field) is not null;
}
