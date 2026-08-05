using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.Modules.Order.Domain;

/// <summary>
/// Well-known errors for the Order domain, exposed as stable <see cref="Error"/> instances so that
/// expected, handleable outcomes are surfaced as results rather than exceptions.
/// </summary>
public static class OrderErrors
{
    public static Error NotFound(PublicId publicId)
        => Error.NotFound("Order.NotFound", $"No order was found with public id '{publicId}'.");

    public static readonly Error CustomerRequired = Error.Validation(
        "Order.CustomerRequired",
        "An order must reference a customer public id.");

    public static readonly Error NoLines = Error.Validation(
        "Order.NoLines",
        "An order must contain at least one line.");

    public static readonly Error InvalidQuantity = Error.Validation(
        "Order.InvalidQuantity",
        "Each order line must have a strictly positive quantity.");

    public static readonly Error InvalidUnitPrice = Error.Validation(
        "Order.InvalidUnitPrice",
        "Each order line must have a non-negative unit price.");

    public static readonly Error ProductNameRequired = Error.Validation(
        "Order.ProductNameRequired",
        "Each order line must capture the product name.");

    public static readonly Error ProductRequired = Error.Validation(
        "Order.ProductRequired",
        "Each order line must reference a product public id.");
}
