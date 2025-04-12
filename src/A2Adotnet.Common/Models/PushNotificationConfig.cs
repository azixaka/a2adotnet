using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Models;

/// <summary>
/// Configuration for push notifications provided by the client to the agent.
/// </summary>
public record PushNotificationConfig
{
    /// <summary>
    /// The endpoint URL for the agent to POST notifications to.
    /// </summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <summary>
    /// Optional token unique to this task/session for the agent to include in the notification.
    /// </summary>
    [JsonPropertyName("token")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Token { get; init; }

    /// <summary>
    /// Optional authentication details the agent needs to use when calling the notification URL.
    /// </summary>
    [JsonPropertyName("authentication")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AgentAuthentication? Authentication { get; init; } // Reuses AgentAuthentication model
}