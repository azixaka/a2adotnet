using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Models;

/// <summary>
/// Parameters for the tasks/get and tasks/resubscribe methods.
/// </summary>
public record TaskQueryParams
{
    /// <summary>
    /// The unique identifier for the task.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Optional hint to the server about how many recent messages to include in the response history.
    /// </summary>
    [JsonPropertyName("historyLength")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? HistoryLength { get; init; }

    /// <summary>
    /// Optional metadata for the task request.
    /// </summary>
    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Metadata { get; init; }
}