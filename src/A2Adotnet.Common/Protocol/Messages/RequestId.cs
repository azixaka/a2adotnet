using System.Diagnostics;
using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Protocol.Messages;

/// <summary>
/// Represents a JSON-RPC request identifier, which can be a string or a number (represented as long).
/// </summary>
[JsonConverter(typeof(RequestIdConverter))]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly struct RequestId : IEquatable<RequestId>
{
    private readonly object _value; // Stores either string or long

    /// <summary>
    /// Gets the string value if the ID is a string, otherwise null.
    /// </summary>
    public string? StringValue => _value as string;

    /// <summary>
    /// Gets the numeric value if the ID is a number, otherwise null.
    /// </summary>
    public long? LongValue => _value is long l ? l : null;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestId"/> struct with a string value.
    /// </summary>
    public RequestId(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestId"/> struct with a numeric value.
    /// </summary>
    public RequestId(long value)
    {
        _value = value;
    }

    // Implicit conversions for convenience
    public static implicit operator RequestId(string value) => new(value);
    public static implicit operator RequestId(long value) => new(value);
    public static implicit operator RequestId(int value) => new(value); // Common case

    public bool Equals(RequestId other) => _value.Equals(other._value);
    public override bool Equals(object? obj) => obj is RequestId other && Equals(other);
    public override int GetHashCode() => _value.GetHashCode();
    public override string ToString() => _value.ToString() ?? string.Empty;

    public static bool operator ==(RequestId left, RequestId right) => left.Equals(right);
    public static bool operator !=(RequestId left, RequestId right) => !(left == right);

    private string DebuggerDisplay => $"RequestId({(_value is string s ? $"\"{s}\"" : _value)})";
}