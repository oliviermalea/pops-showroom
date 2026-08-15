namespace ShowRoom.Modules.Order.Features.OnCustomerRegistered;

/// <summary>
/// Thrown when a <c>CustomerRegistered</c> message cannot be processed because it is missing required data
/// (a "poison" message). It is the signal the retry/dead-letter policy keys on: after a few retries the
/// message is moved to the Wolverine dead-letter store instead of being retried forever
/// (see <see cref="OnCustomerRegisteredMessaging"/>).
/// </summary>
public sealed class UnprocessableCustomerRegisteredException(string email)
    : Exception($"The CustomerRegistered event for '{email}' is missing required data and cannot be processed.");
