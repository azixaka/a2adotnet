using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Protocol.Messages;

/// <summary>
/// Represents the 'error' object within a JSON-RPC error response.
/// </summary>
public record JsonRpcErrorDetail
{
    /// <summary>
    /// A Number that indicates the error type that occurred.
    /// </summary>
    [JsonPropertyName("code")]
    public required int Code { get; init; }

    /// <summary>
    /// A String providing a short description of the error.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// A Primitive or Structured value that contains additional information about the error.
    /// Can be omitted.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; init; }
}