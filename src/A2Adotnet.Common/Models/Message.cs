using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Models;

/// <summary>
/// Represents a communication unit between a user and an agent within a Task.
/// </summary>
public record Message
{
    /// <summary>
    /// The role of the sender ("user" or "agent").
    /// </summary>
    [JsonPropertyName("role")]
    public required string Role { get; init; } // Consider an enum later if needed

    /// <summary>
    /// The content parts of the message.
    /// </summary>
    [JsonPropertyName("parts")]
    public required List<Part> Parts { get; init; }

    /// <summary>
    /// Optional message-specific metadata.
    /// </summary>
    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Metadata { get; init; }

    // Constructor for convenience
    public Message(string role, List<Part> parts, Dictionary<string, object>? metadata = null)
    {
        // Basic validation
        if (role != "user" && role != "agent")
        {
            throw new ArgumentException("Role must be 'user' or 'agent'.", nameof(role));
        }
        ArgumentNullException.ThrowIfNull(parts);
        if (parts.Count == 0)
        {
             throw new ArgumentException("Parts list cannot be empty.", nameof(parts));
        }

        Role = role;
        Parts = parts;
        Metadata = metadata;
    }
}