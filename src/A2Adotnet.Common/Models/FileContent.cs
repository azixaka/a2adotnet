using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Models;

/// <summary>
/// Represents the content of a file, either as base64 encoded bytes or a URI.
/// Ensures that either 'Bytes' or 'Uri' is provided, but not both (validation handled during construction/usage).
/// </summary>
public record FileContent
{
    /// <summary>
    /// Optional filename.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; }

    /// <summary>
    /// Optional MIME type of the file content.
    /// </summary>
    [JsonPropertyName("mimeType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MimeType { get; init; }

    /// <summary>
    /// Base64 encoded file content. Mutually exclusive with Uri.
    /// </summary>
    [JsonPropertyName("bytes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Bytes { get; init; }

    /// <summary>
    /// URI pointing to the file content. Mutually exclusive with Bytes.
    /// </summary>
    [JsonPropertyName("uri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Uri { get; init; }

    // Consider adding validation in constructor or factory methods later
    // to enforce mutual exclusivity of Bytes and Uri if strictness is needed at creation time.
}