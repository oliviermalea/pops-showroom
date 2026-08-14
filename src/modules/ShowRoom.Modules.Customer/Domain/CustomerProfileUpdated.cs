using ShowRoom.BuildingBlocks.Domain.Primitives;
using ShowRoom.BuildingBlocks.Domain.PublicIds;

namespace ShowRoom.Modules.Customer.Domain;

/// <summary>
/// Coarse "Style 2" domain event: raised by <see cref="Customer.UpdateProfile"/> when a profile edit
/// actually changes something. A single event stands for the whole edit; <see cref="ChangedFields"/>
/// (e.g. <c>["Name", "Email"]</c>) keeps just enough granularity for a consumer to branch without
/// subscribing to per-field events. Contrast with the fine-grained <see cref="CustomerRenamed"/> /
/// <see cref="CustomerEmailChanged"/> / <see cref="CustomerPhoneChanged"/> (Style 1).
/// </summary>
public sealed record CustomerProfileUpdated(
    Guid Id,
    CustomerId CustomerId,
    PublicId PublicId,
    IReadOnlyCollection<string> ChangedFields) : DomainEvent(Id);
