using ShowRoom.Web.Infrastructure.Api.Refit.Customer.Models;

namespace ShowRoom.Web.Features.Customer.CreateCustomer;

/// <summary>Maps the form model to the API contract: trims every input, and drops an empty phone.</summary>
public static class CreateCustomerMapper
{
    public static CreateCustomerRequest ToApi(CreateCustomerForm form)
    {
        ArgumentNullException.ThrowIfNull(form);

        return new CreateCustomerRequest(
            FirstName: form.FirstName.Trim(),
            LastName: form.LastName.Trim(),
            Email: form.Email.Trim(),
            Phone: string.IsNullOrWhiteSpace(form.Phone) ? null : form.Phone.Trim());
    }
}
