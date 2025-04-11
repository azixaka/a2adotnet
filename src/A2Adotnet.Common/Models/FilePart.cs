using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Models;

/// <summary>
/// Represents a file content part within a Message or Artifact.
/// </summary>
public record FilePart : Part
{
    /// <summary>
    /// The file content details (bytes or URI).
    /// </summary>
    [JsonPropertyName("file")]
    public required FileContent File { get; init; }

    // Constructor for convenience
    public FilePart(FileContent file, Dictionary<string, object>? metadata = null)
        : base("file", metadata)
    {
        ArgumentNullException.ThrowIfNull(file);
        File = file;
    }
}