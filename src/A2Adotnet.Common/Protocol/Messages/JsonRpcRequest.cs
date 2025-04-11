using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Protocol.Messages;

/// <summary>
/// Represents a generic JSON-RPC request message.
/// </summary>
/// <typeparam name="TParams">The type of the parameters object.</typeparam>
public record JsonRpcRequest<TParams> : IJsonRpcMessageWithId where TParams : class
{
    /// <summary>
    /// JSON-RPC protocol version. Always "2.0".
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";

    /// <summary>
    /// Request identifier. Must be a string or number.
    /// </summary>
    [JsonPropertyName("id")]
    public RequestId? Id { get; init; } // Made nullable to match interface

    /// <summary>
    /// Name of the method to invoke (e.g., "tasks/send").
    /// </summary>
    [JsonPropertyName("method")]
    public required string Method { get; init; }

    /// <summary>
    /// Parameters for the method.
    /// </summary>
    [JsonPropertyName("params")]
    public required TParams Params { get; init; }
}