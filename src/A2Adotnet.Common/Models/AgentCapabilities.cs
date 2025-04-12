using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Models;

/// <summary>
/// Represents the capabilities supported by an agent.
/// </summary>
public record AgentCapabilities
{
    /// <summary>
    /// Indicates if the agent supports SSE streaming via `tasks/sendSubscribe`. Defaults to false.
    /// </summary>
    [JsonPropertyName("streaming")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Streaming { get; init; } = false;

    /// <summary>
    /// Indicates if the agent supports push notifications via `tasks/pushNotification/set|get`. Defaults to false.
    /// </summary>
    [JsonPropertyName("pushNotifications")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool PushNotifications { get; init; } = false;

    /// <summary>
    /// Indicates if the agent supports providing detailed state transition history. Defaults to false.
    /// </summary>
    [JsonPropertyName("stateTransitionHistory")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool StateTransitionHistory { get; init; } = false;
}