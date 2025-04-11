using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Protocol.Messages;

/// <summary>
/// Base interface for all JSON-RPC messages.
/// </summary>
public interface IJsonRpcMessage
{
    /// <summary>
    /// JSON-RPC protocol version. Must be "2.0".
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    string JsonRpc { get; }
}