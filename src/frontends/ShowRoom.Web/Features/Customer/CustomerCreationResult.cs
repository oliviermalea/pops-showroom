using ShowRoom.Web.Infrastructure.Api.Problems;

namespace ShowRoom.Web.Features.Customer;

/// <summary>Outcome of a customer creation, as the UI needs to distinguish it.</summary>
public enum CustomerCreationOutcome
{
    /// <summary>The customer was created; <see cref="CustomerCreationResult.PublicId"/> carries its id.</summary>
    Created,

    /// <summary>Another customer already uses this email (backend answered 409).</summary>
    EmailAlreadyUsed,

    /// <summary>The backend rejected the payload (400) — client-side rules let something through.</summary>
    Rejected,

    /// <summary>The Customer service could not be reached, or answered an unexpected status.</summary>
    Unavailable,
}

/// <summary>Result of a customer creation: an explicit outcome plus the created public id on success.</summary>
public sealed record CustomerCreationResult(
    CustomerCreationOutcome Outcome,
    string? PublicId,
    ApiProblem? Problem = null)
{
    public static CustomerCreationResult Created(string publicId) => new(CustomerCreationOutcome.Created, publicId);

    public static CustomerCreationResult EmailAlreadyUsed(ApiProblem? problem = null)
        => new(CustomerCreationOutcome.EmailAlreadyUsed, null, problem);

    public static CustomerCreationResult Rejected(ApiProblem? problem = null)
        => new(CustomerCreationOutcome.Rejected, null, problem);

    public static CustomerCreationResult Unavailable(ApiProblem? problem = null)
        => new(CustomerCreationOutcome.Unavailable, null, problem);
}
