using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Models;

/// Represents a task artifact update event received via SSE during tasks/sendSubscribe or tasks/resubscribe.
/// </summary>
public record TaskArtifactUpdateEvent : TaskUpdateEventBase
{
    /// <summary>
    /// The ID of the task being updated.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// The artifact data being sent.
    /// </summary>
    [JsonPropertyName("artifact")]
    public required Artifact Artifact { get; init; }

    /// <summary>
    /// Optional metadata associated with the event.
    /// </summary>
    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Metadata { get; init; }

    // Note: The 'final' property from the spec seems more relevant to TaskStatusUpdateEvent
    // to signal the end of the entire stream, rather than per-artifact.
    // If needed per-artifact, it could be added here, but Artifact.LastChunk might suffice.
}