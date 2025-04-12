using A2Adotnet.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace A2Adotnet.Server.Push;

/// <summary>
/// Implements IPushNotificationSender using HttpClient.
/// </summary>
internal class HttpPushNotificationSender : IPushNotificationSender
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpPushNotificationSender> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Define a specific HttpClient name for potential configuration
    private const string PushHttpClientName = "A2APushNotificationClient";

    public HttpPushNotificationSender(
        IHttpClientFactory httpClientFactory,
        ILogger<HttpPushNotificationSender> logger,
        IOptions<JsonSerializerOptions>? jsonOptions = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _jsonOptions = jsonOptions?.Value ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            Converters = { new Common.Protocol.Messages.RequestIdConverter() /* Add Part converter if needed */ }
        };
    }

    public async System.Threading.Tasks.Task SendNotificationAsync(PushNotificationConfig config, Common.Models.Task task, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(task);

        if (!Uri.TryCreate(config.Url, UriKind.Absolute, out var targetUri))
        {
            _logger.LogError("Invalid push notification URL for Task ID {TaskId}: {Url}", task.Id, config.Url);
            return; // Or throw? Invalid config provided by client.
        }

        _logger.LogInformation("Sending push notification for Task ID {TaskId} to {Url}", task.Id, config.Url);

        try
        {
            var httpClient = _httpClientFactory.CreateClient(PushHttpClientName);

            using var request = new HttpRequestMessage(HttpMethod.Post, targetUri);

            // Add authentication header if specified in config
            // This part needs refinement based on supported schemes (Bearer, JWT, etc.)
            if (config.Authentication != null)
            {
                // Example: Bearer token (assuming credentials contain the token)
                if (config.Authentication.Schemes.Contains("bearer", StringComparer.OrdinalIgnoreCase) && !string.IsNullOrEmpty(config.Authentication.Credentials))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.Authentication.Credentials);
                }
                // TODO: Add support for other schemes like JWT generation/signing if needed
                else
                {
                     _logger.LogWarning("Unsupported or missing credentials for push notification authentication scheme(s) {Schemes} for Task ID {TaskId}",
                        string.Join(",", config.Authentication.Schemes), task.Id);
                }
            }

            // Add optional task-specific token if provided
            if (!string.IsNullOrEmpty(config.Token))
            {
                // Add as a custom header? Or part of payload? Spec is unclear. Let's use a header.
                request.Headers.TryAddWithoutValidation("X-A2A-Push-Token", config.Token);
            }

            // Set content - send the full Task object as JSON
            request.Content = JsonContent.Create(task, typeof(Common.Models.Task), options: _jsonOptions);

            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Push notification failed for Task ID {TaskId} to {Url}. Status: {StatusCode}. Response: {ResponseBody}",
                    task.Id, config.Url, response.StatusCode, responseBody);
                // Implement retry logic? Based on status code?
            }
            else
            {
                 _logger.LogInformation("Push notification sent successfully for Task ID {TaskId} to {Url}", task.Id, config.Url);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error sending push notification for Task ID {TaskId} to {Url}", task.Id, config.Url);
        }
        catch (OperationCanceledException)
        {
             _logger.LogInformation("Push notification sending cancelled for Task ID {TaskId}", task.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending push notification for Task ID {TaskId} to {Url}", task.Id, config.Url);
        }
    }
}