using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Models;

/// <summary>
/// Represents a specific skill or capability that an agent can perform.
/// </summary>
public record AgentSkill
{
    /// <summary>
    /// Unique identifier for the agent's skill.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable name of the skill.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Optional description of the skill. Used as a hint for clients/users.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }

    /// <summary>
    /// Optional set of tag words describing classes of capabilities for this skill (e.g., "billing", "image-generation").
    /// </summary>
    [JsonPropertyName("tags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Tags { get; init; }

    /// <summary>
    /// Optional set of example scenarios or prompts for this skill.
    /// </summary>
    [JsonPropertyName("examples")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Examples { get; init; }

    /// <summary>
    /// Optional list of input modes (e.g., MIME types) supported specifically by this skill, overriding agent defaults.
    /// </summary>
    [JsonPropertyName("inputModes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? InputModes { get; init; }

    /// <summary>
    /// Optional list of output modes (e.g., MIME types) supported specifically by this skill, overriding agent defaults.
    /// </summary>
    [JsonPropertyName("outputModes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? OutputModes { get; init; }
}