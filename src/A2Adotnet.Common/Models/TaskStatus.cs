using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Models;

/// <summary>
/// Represents the status of a Task at a specific point in time.
/// </summary>
public record TaskStatus
{
    /// <summary>
    /// The current lifecycle state of the task.
    /// </summary>
    [JsonPropertyName("state")]
    public required TaskState State { get; init; }

    /// <summary>
    /// Optional message associated with this status update (e.g., progress info, input prompt, final response text).
    /// </summary>
    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Message? Message { get; init; }

    /// <summary>
    /// ISO 8601 timestamp of when this status was recorded.
    /// Automatically set on creation if not provided.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    // Removed constructor to allow object initializers with required members
}