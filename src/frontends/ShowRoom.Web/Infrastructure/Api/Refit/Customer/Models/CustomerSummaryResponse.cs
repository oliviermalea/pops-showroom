namespace ShowRoom.Web.Infrastructure.Api.Refit.Customer.Models;

/// <summary>
/// Wire contract of one item of <c>GET /api/v1/customers</c>. Compact projection: no phone, and the
/// customer is identified by its <see cref="PublicId"/> only.
/// </summary>
public sealed record CustomerSummaryResponse(
    string PublicId,
    string FirstName,
    string LastName,
    string DisplayName,
    string Email,
    string Status,
    DateTimeOffset RegisteredOn);
