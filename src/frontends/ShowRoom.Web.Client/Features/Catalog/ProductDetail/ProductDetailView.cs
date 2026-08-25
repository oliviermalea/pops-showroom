namespace ShowRoom.Web.Client.Features.Catalog.ProductDetail;

/// <summary>
/// View model of the product detail screen: everything is already presentable, so the component
/// formats nothing.
/// </summary>
public sealed record ProductDetailView(
    string PublicId,
    string Name,
    string Description,
    string Price,
    string Currency,
    string Status,
    bool IsAvailable);
