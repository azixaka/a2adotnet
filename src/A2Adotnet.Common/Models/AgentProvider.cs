using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Models;

/// <summary>
/// Represents the provider of an agent.
/// </summary>
public record AgentProvider
{
    /// <summary>
    /// The organization providing the agent.
    /// </summary>
    [JsonPropertyName("organization")]
    public required string Organization { get; init; }

    /// <summary>
    /// Optional URL for the provider organization.
    /// </summary>
    [JsonPropertyName("url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; init; }
}