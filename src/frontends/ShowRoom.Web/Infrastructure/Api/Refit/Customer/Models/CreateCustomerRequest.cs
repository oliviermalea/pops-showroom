namespace ShowRoom.Web.Infrastructure.Api.Refit.Customer.Models;

/// <summary>
/// Wire contract of <c>POST /api/v1/customers</c> (ShowRoom.Customer.Api). Mirrors the backend
/// <c>CreateCustomerCommand</c>; the response carries the created <c>PublicId</c> as a string.
/// </summary>
public sealed record CreateCustomerRequest(
    string FirstName,
    string LastName,
    string Email,
    string? Phone);
