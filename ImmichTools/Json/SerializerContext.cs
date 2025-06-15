using System.Text.Json.Serialization;

namespace ImmichTools.Json;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(RequestData.CreateStack))]
[JsonSerializable(typeof(RequestData.UpdateAsset))]
[JsonSerializable(typeof(ReplyData.Asset))]
[JsonSerializable(typeof(ReplyData.Asset[]))]
[JsonSerializable(typeof(ReplyData.ExifInfo))]
[JsonSerializable(typeof(string[]))]
internal partial class SerializerContext : JsonSerializerContext
{
}
