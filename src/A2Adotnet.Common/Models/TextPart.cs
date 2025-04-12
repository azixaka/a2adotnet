using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Models;

/// <summary>
/// Represents a text content part within a Message or Artifact.
/// </summary>
public record TextPart : Part
{
    /// <summary>
    /// The textual content.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; init; } // Removed required

    // Constructor for convenience
    public TextPart(string text, Dictionary<string, object>? metadata = null)
        : base("text", metadata)
    {
        Text = text;
    }
}