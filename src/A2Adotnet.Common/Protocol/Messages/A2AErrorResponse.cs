using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Protocol.Messages;

/// <summary>
/// Represents a JSON-RPC error response.
/// </summary>
public record A2AErrorResponse : IJsonRpcMessageWithId
{
    /// <summary>
    /// JSON-RPC protocol version. Always "2.0".
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";

    /// <summary>
    /// Request identifier matching the original request. Can be null if the ID couldn't be determined (e.g., parse error).
    /// </summary>
    [JsonPropertyName("id")]
    public RequestId? Id { get; init; }

    /// <summary>
    /// The error object.
    /// </summary>
    [JsonPropertyName("error")]
    public required JsonRpcErrorDetail Error { get; init; }
}