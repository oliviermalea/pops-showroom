using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.BuildingBlocks.Domain.PublicIds;

public static class PublicIdPrefixes
{
    public const string Lead = "lea";
    public const string InformationRequest = "inf";
    public const string ContentNode = "con";
    public const string Customer = "cus";
    public const string Order = "ord";
}

public static class PublicIdFactory
{
    public static Result<PublicId> ForLead()
        => PublicId.Create(PublicIdPrefixes.Lead);

    public static Result<PublicId> ForInformationRequest()
        => PublicId.Create(PublicIdPrefixes.InformationRequest);

    public static Result<PublicId> ForContentNode()
        => PublicId.Create(PublicIdPrefixes.ContentNode);

    public static Result<PublicId> ForCustomer()
        => PublicId.Create(PublicIdPrefixes.Customer);

    public static Result<PublicId> ForOrder()
        => PublicId.Create(PublicIdPrefixes.Order);
}
