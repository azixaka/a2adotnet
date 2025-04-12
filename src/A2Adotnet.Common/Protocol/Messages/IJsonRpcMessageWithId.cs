using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Protocol.Messages;

/// <summary>
/// Interface for JSON-RPC messages that include an identifier.
/// </summary>
public interface IJsonRpcMessageWithId : IJsonRpcMessage
{
    /// <summary>
    /// Request identifier. Must be a string or number (or null for error responses where the ID couldn't be determined).
    /// </summary>
    [JsonPropertyName("id")]
    RequestId? Id { get; } // Allow null for certain error cases
}