using ShowRoom.Web.Infrastructure.Api.Refit.Customer.Models;
using ShowRoom.Web.Shared.Api.Models;

namespace ShowRoom.Web.Features.Customer.CustomerList;

/// <summary>Maps the paginated API contract to the list screen's view model.</summary>
public static class CustomerListMapper
{
    public static CustomerListView FromApi(PagedResponse<CustomerSummaryResponse> response)
    {
        ArgumentNullException.ThrowIfNull(response);

        var items = (response.Items ?? [])
            .Select(ToItem)
            .ToList();

        return new CustomerListView(
            Items: items,
            Page: response.Page,
            PageSize: response.PageSize,
            TotalItems: response.TotalItems,
            TotalPages: response.TotalPages);
    }

    private static CustomerListItemView ToItem(CustomerSummaryResponse summary) => new(
        PublicId: summary.PublicId,
        DisplayName: CustomerFormat.DisplayName(summary.DisplayName, summary.FirstName, summary.LastName),
        Email: summary.Email,
        Status: summary.Status,
        IsActive: CustomerFormat.IsActive(summary.Status),
        RegisteredOn: CustomerFormat.Date(summary.RegisteredOn));
}
