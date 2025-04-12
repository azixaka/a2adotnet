using A2Adotnet.Common.Models;

namespace A2Adotnet.Server.Push;

/// <summary>
/// Interface for sending push notifications (webhooks) to client-specified URLs.
/// </summary>
public interface IPushNotificationSender
{
    /// <summary>
    /// Sends a notification containing the updated task state to the configured URL.
    /// </summary>
    /// <param name="config">The push notification configuration containing the URL and authentication details.</param>
    /// <param name="task">The updated task object to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A System.Threading.Tasks.Task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Implementations should handle authentication based on the config
    /// and potentially implement retry logic or error handling.
    /// </remarks>
    System.Threading.Tasks.Task SendNotificationAsync(PushNotificationConfig config, Common.Models.Task task, CancellationToken cancellationToken = default);
}