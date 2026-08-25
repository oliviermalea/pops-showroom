using ShowRoom.Web.Client.Infrastructure.Api.Refit.Catalog.Models;
using ShowRoom.Web.Shared.Api.Models;

namespace ShowRoom.Web.Client.Features.Catalog.ProductList;

/// <summary>Maps the paginated API contract to the catalogue screen's view model.</summary>
public static class ProductListMapper
{
    public static ProductListView FromApi(PagedResponse<ProductSummaryResponse> response)
    {
        ArgumentNullException.ThrowIfNull(response);

        var items = (response.Items ?? [])
            .Select(ToItem)
            .ToList();

        return new ProductListView(
            Items: items,
            Page: response.Page,
            PageSize: response.PageSize,
            TotalItems: response.TotalItems,
            TotalPages: response.TotalPages);
    }

    private static ProductListItemView ToItem(ProductSummaryResponse summary) => new(
        PublicId: summary.PublicId,
        Name: CatalogFormat.OrPlaceholder(summary.Name),
        Price: CatalogFormat.Money(summary.Price, summary.Currency),
        Status: CatalogFormat.OrPlaceholder(summary.Status),
        IsAvailable: CatalogFormat.IsAvailable(summary.Status));
}
