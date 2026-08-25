using ShowRoom.Web.Client.Infrastructure.Api.Refit.Catalog.Models;

namespace ShowRoom.Web.Client.Features.Catalog.ProductDetail;

/// <summary>Maps the API contract to the detail screen's view model.</summary>
public static class ProductDetailMapper
{
    public static ProductDetailView FromApi(ProductResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new ProductDetailView(
            PublicId: response.PublicId,
            Name: CatalogFormat.OrPlaceholder(response.Name),
            Description: CatalogFormat.OrPlaceholder(response.Description),
            Price: CatalogFormat.Money(response.Price, response.Currency),
            Currency: CatalogFormat.OrPlaceholder(response.Currency),
            Status: CatalogFormat.OrPlaceholder(response.Status),
            IsAvailable: CatalogFormat.IsAvailable(response.Status));
    }
}
