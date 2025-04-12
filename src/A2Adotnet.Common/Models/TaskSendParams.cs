using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Models;

/// <summary>
/// Parameters for the tasks/send and tasks/sendSubscribe methods.
/// </summary>
public record TaskSendParams
{
    /// <summary>
    /// The unique identifier for the task.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Optional client-generated identifier for the session holding the task.
    /// If not set for a new task ID, the server may generate one.
    /// </summary>
    [JsonPropertyName("sessionId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SessionId { get; init; }

    /// <summary>
    /// The message payload to send to the agent.
    /// </summary>
    [JsonPropertyName("message")]
    public required Message Message { get; init; }

    /// <summary>
    /// Optional push notification configuration for the server to use for this task.
    /// </summary>
    [JsonPropertyName("pushNotification")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PushNotificationConfig? PushNotification { get; init; } // Assumes PushNotificationConfig is defined

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