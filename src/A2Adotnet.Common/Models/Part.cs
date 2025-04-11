using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Models;

/// <summary>
/// Represents a piece of content within a Message or Artifact.
/// This is the base record for different part types (Text, File, Data).
/// </summary>
[JsonDerivedType(typeof(TextPart), typeDiscriminator: "text")]
[JsonDerivedType(typeof(FilePart), typeDiscriminator: "file")]
[JsonDerivedType(typeof(DataPart), typeDiscriminator: "data")]
public abstract record Part
{
    /// <summary>
    /// The type of the part, used as the discriminator for polymorphic deserialization.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; } // Made required for clarity, though set by derived types

    /// <summary>
    /// Optional metadata for the specific part.
    /// </summary>
    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Metadata { get; init; }

    // Protected constructor for derived types
    protected Part(string type, Dictionary<string, object>? metadata = null)
    {
        Type = type;
        Metadata = metadata;
    }
}