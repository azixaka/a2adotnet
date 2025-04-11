using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Models;

/// <summary>
/// Base record for events received via SSE stream (TaskStatusUpdateEvent or TaskArtifactUpdateEvent).
/// This helps in using a single type for IAsyncEnumerable results.
/// </summary>
// No JsonDerivedType needed here as we parse based on the SSE 'event:' field, not a JSON discriminator.
public abstract record TaskUpdateEventBase
{
    /// <summary>
    /// The ID of the task being updated.
    /// </summary>
    [JsonPropertyName("id")] // Common property, but defined in derived types for 'required'
    public abstract string Id { get; init; }

    /// <summary>
    /// Optional metadata associated with the event.
    /// </summary>
    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Metadata { get; init; }

    // Make constructor protected
    protected TaskUpdateEventBase() { }
}

// Add inheritance to existing event types

// Modify TaskStatusUpdateEvent.cs to inherit from TaskUpdateEventBase
// (This requires apply_diff as the file exists)

// Modify TaskArtifactUpdateEvent.cs to inherit from TaskUpdateEventBase
// (This requires apply_diff as the file exists)