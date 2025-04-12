using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Models;

/// <summary>
/// Represents the metadata describing an agent, typically found at `/.well-known/agent.json`.
/// </summary>
public record AgentCard
{
    /// <summary>
    /// Human-readable name of the agent (e.g., "Recipe Agent").
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Optional human-readable description of the agent.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }

    /// <summary>
    /// The base URL endpoint for the agent's A2A service.
    /// </summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <summary>
    /// Optional details about the agent's provider.
    /// </summary>
    [JsonPropertyName("provider")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AgentProvider? Provider { get; init; }

    /// <summary>
    /// The version of the agent or its API (provider-defined format).
    /// </summary>
    [JsonPropertyName("version")]
    public required string Version { get; init; }

    /// <summary>
    /// Optional URL to documentation for the agent.
    /// </summary>
    [JsonPropertyName("documentationUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DocumentationUrl { get; init; }

    /// <summary>
    /// Capabilities supported by the agent (streaming, push notifications, etc.).
    /// </summary>
    [JsonPropertyName("capabilities")]
    public required AgentCapabilities Capabilities { get; init; }

    /// <summary>
    /// Optional authentication requirements for the agent.
    /// </summary>
    [JsonPropertyName("authentication")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AgentAuthentication? Authentication { get; init; }

    /// <summary>
    /// Default input modes (e.g., MIME types) supported across all skills. Defaults to ["text"].
    /// </summary>
    [JsonPropertyName("defaultInputModes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? DefaultInputModes { get; init; } = new List<string> { "text" };

    /// <summary>
    /// Default output modes (e.g., MIME types) supported across all skills. Defaults to ["text"].
    /// </summary>
    [JsonPropertyName("defaultOutputModes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? DefaultOutputModes { get; init; } = new List<string> { "text" };

    /// <summary>
    /// List of specific skills the agent possesses.
    /// </summary>
    [JsonPropertyName("skills")]
    public required List<AgentSkill> Skills { get; init; }
}