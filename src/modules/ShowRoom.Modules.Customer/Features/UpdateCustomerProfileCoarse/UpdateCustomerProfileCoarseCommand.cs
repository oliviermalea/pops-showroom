namespace ShowRoom.Modules.Customer.Features.UpdateCustomerProfileCoarse;

/// <summary>
/// Command for the "Style 2" coarse profile update: one operation, one <c>CustomerProfileUpdated</c>
/// event. Compare with <c>UpdateCustomerProfileCommand</c> (Style 1, task-based / fine-grained events).
/// </summary>
public sealed record UpdateCustomerProfileCoarseCommand(
    string PublicId,
    string FirstName,
    string LastName,
    string Email,
    string? Phone);
