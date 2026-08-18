namespace ShowRoom.Web.Infrastructure.Api.Refit.Customer.Models;

/// <summary>
/// Wire contract of <c>GET /api/v1/customers/{publicId}</c> (ShowRoom.Customer.Api). Mirrors the
/// backend <c>CustomerResponse</c>: the customer is identified by its <see cref="PublicId"/> only,
/// never by a technical identifier.
/// </summary>
public sealed record CustomerDetailResponse(
    string PublicId,
    string FirstName,
    string LastName,
    string DisplayName,
    string Email,
    string? Phone,
    string Status,
    DateTimeOffset RegisteredOn);
