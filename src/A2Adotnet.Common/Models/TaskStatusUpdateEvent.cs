using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Models;

/// Represents a task status update event received via SSE during tasks/sendSubscribe or tasks/resubscribe.
/// </summary>
public record TaskStatusUpdateEvent : TaskUpdateEventBase
{
    /// <summary>
    /// The ID of the task being updated.
    /// </summary>
    [JsonPropertyName("id")]
    public override required string Id { get; init; } // Added override

    /// <summary>
    /// The new status object for the task.
    /// </summary>
    [JsonPropertyName("status")]
    public required TaskStatus Status { get; init; }

    /// <summary>
    /// Indicates if this is the terminal update for the task stream. Defaults to false.
    /// </summary>
    [JsonPropertyName("final")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Final { get; init; } = false;

    /// <summary>
    /// Optional metadata associated with the event.
    /// </summary>
    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Metadata { get; init; }
}