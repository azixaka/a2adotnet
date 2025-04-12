using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace A2Adotnet.Client;

/// <summary>
/// Extension methods for setting up A2A client services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class A2AClientServiceCollectionExtensions
{
    /// <summary>
    /// Adds the <see cref="IA2AClient"/> and related services to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configureOptions">An optional action to configure the <see cref="A2AClientOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddA2AClient(
        this IServiceCollection services,
        Action<A2AClientOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Add IHttpClientFactory if not already added
        services.AddHttpClient();

        // Configure options
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        // Add the client implementation
        // Use TryAdd to allow users to register a custom HttpClient configuration for IA2AClient
        services.TryAddTransient<IA2AClient>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            // Create a client instance. Users can configure this named client ("A2AClient")
            // using AddHttpClient("A2AClient").ConfigurePrimaryHttpMessageHandler(...) etc.
            var httpClient = httpClientFactory.CreateClient("A2AClient");
            var options = sp.GetRequiredService<IOptions<A2AClientOptions>>();
            return new A2AClient(httpClient, options);
        });

        return services;
    }
}