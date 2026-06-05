using System.Text.Json;

namespace Phoenixd.NET.Json;

/// <summary>
/// Shared <see cref="JsonSerializerOptions"/> used for every phoenixd payload (HTTP responses and
/// websocket events). phoenixd serializes everything in camelCase, so the web defaults
/// (camelCase naming + case-insensitive matching) line up with the PascalCase C# models without
/// per-property attributes. Reusing a single cached instance also avoids the cost of building new
/// options on every (de)serialization call.
/// </summary>
public static class PhoenixdJson
{
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = true,
    };
}
