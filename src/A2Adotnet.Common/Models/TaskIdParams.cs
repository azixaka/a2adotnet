using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Models;

/// <summary>
/// Parameters for methods that only require a Task ID, like tasks/cancel or tasks/pushNotification/get.
/// </summary>
public record TaskIdParams
{
    /// <summary>
    /// The unique identifier for the task.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Optional metadata for the task request.
    /// </summary>
    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Metadata { get; init; }
}