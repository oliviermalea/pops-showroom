using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.Modules.Product.Domain;

/// <summary>
/// Well-known errors for the Product domain, exposed as stable <see cref="Error"/> instances so that
/// expected, handleable outcomes are surfaced as results rather than exceptions.
/// </summary>
public static class ProductErrors
{
    public static Error NotFound(PublicId publicId)
        => Error.NotFound("Product.NotFound", $"No product was found with public id '{publicId}'.");

    public static readonly Error NameRequired = Error.Validation(
        "Product.NameRequired",
        "A product must have a name.");

    public static readonly Error InvalidPrice = Error.Validation(
        "Product.InvalidPrice",
        "A product must have a non-negative price.");
}
