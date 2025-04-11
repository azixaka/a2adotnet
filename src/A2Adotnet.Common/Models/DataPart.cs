using System.Text.Json.Nodes; // Using JsonNode for flexibility
using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Models;

/// <summary>
/// Represents a structured JSON data part within a Message or Artifact (e.g., for forms).
/// </summary>
public record DataPart : Part
{
    /// <summary>
    /// The structured JSON data. Using JsonNode allows representing any valid JSON structure.
    /// </summary>
    [JsonPropertyName("data")]
    public required JsonNode Data { get; init; } // Use JsonNode for arbitrary JSON

    // Constructor for convenience
    public DataPart(JsonNode data, Dictionary<string, object>? metadata = null)
        : base("data", metadata)
    {
        ArgumentNullException.ThrowIfNull(data);
        Data = data;
    }
}