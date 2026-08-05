using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShowRoom.BuildingBlocks.Domain.PublicIds;

// Applies the converter to PublicId via a partial declaration, without touching PublicId.cs,
// so it serialises as its string value (e.g. "cus_ab12...") over HTTP instead of an object.
[JsonConverter(typeof(PublicIdJsonConverter))]
public sealed partial record PublicId;

/// <summary>Serialises <see cref="PublicId"/> as its string value.</summary>
public sealed class PublicIdJsonConverter : JsonConverter<PublicId>
{
    public override PublicId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value is null ? null : PublicId.Parse(value);
    }

    public override void Write(Utf8JsonWriter writer, PublicId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}
