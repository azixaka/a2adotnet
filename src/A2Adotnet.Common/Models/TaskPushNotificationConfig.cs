using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Models;

/// <summary>
/// Associates a PushNotificationConfig with a specific Task ID.
/// Used as parameters for tasks/pushNotification/set and result for tasks/pushNotification/get.
/// </summary>
public record TaskPushNotificationConfig
{
    /// <summary>
    /// The unique identifier for the task.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// The push notification configuration for the specified task.
    /// </summary>
    [JsonPropertyName("pushNotificationConfig")]
    public required PushNotificationConfig PushNotificationConfig { get; init; }
}