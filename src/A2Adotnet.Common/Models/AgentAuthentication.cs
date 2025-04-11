using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Models;

/// <summary>
/// Represents the authentication requirements for an agent or a push notification endpoint.
/// </summary>
public record AgentAuthentication
{
    /// <summary>
    /// List of supported authentication schemes (e.g., "Bearer", "OAuth2", "ApiKey").
    /// </summary>
    [JsonPropertyName("schemes")]
    public required List<string> Schemes { get; init; }

    /// <summary>
    /// Optional credentials information, format depends on the scheme.
    /// For example, could be an API key value or configuration details for OAuth.
    /// Use with caution, especially in publicly accessible Agent Cards.
    /// </summary>
    [JsonPropertyName("credentials")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Credentials { get; init; } // Consider a more structured type if needed later
}