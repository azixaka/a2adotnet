using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Protocol.Messages;

/// <summary>
/// Represents a successful JSON-RPC response.
/// </summary>
/// <typeparam name="TResult">The type of the result object.</typeparam>
public record A2AResponse<TResult> : IJsonRpcMessageWithId
{
    /// <summary>
    /// JSON-RPC protocol version. Always "2.0".
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";

    /// <summary>
    /// Request identifier matching the original request.
    /// </summary>
    [JsonPropertyName("id")]
    public RequestId? Id { get; init; } // Made nullable to match interface

    /// <summary>
    /// The result of the method invocation. Can be null.
    /// </summary>
    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] // Result can be null or omitted
    public TResult? Result { get; init; }
}