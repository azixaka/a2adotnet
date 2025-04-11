using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Models;

/// <summary>
/// Represents the lifecycle state of a Task.
/// </summary>
[JsonConverter(typeof(JsonStringEnumMemberConverter))] // Use EnumMember value for serialization
public enum TaskState
{
    /// <summary>
    /// Task has been received but not yet started.
    /// </summary>
    [EnumMember(Value = "submitted")]
    Submitted,

    /// <summary>
    /// Task is actively being processed.
    /// </summary>
    [EnumMember(Value = "working")]
    Working,

    /// <summary>
    /// Agent requires further input from the client/user.
    /// </summary>
    [EnumMember(Value = "input-required")]
    InputRequired,

    /// <summary>
    /// Task finished successfully.
    /// </summary>
    [EnumMember(Value = "completed")]
    Completed,

    /// <summary>
    /// Task was canceled.
    /// </summary>
    [EnumMember(Value = "canceled")]
    Canceled,

    /// <summary>
    /// Task failed due to an error.
    /// </summary>
    [EnumMember(Value = "failed")]
    Failed,

    /// <summary>
    /// Task state cannot be determined.
    /// </summary>
    [EnumMember(Value = "unknown")]
    Unknown
}