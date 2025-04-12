using Microsoft.Extensions.DependencyInjection;

namespace A2Adotnet.Server.Builder;

/// <summary>
/// A builder abstraction for configuring A2A server components.
/// </summary>
public interface IA2AServerBuilder
{
    /// <summary>
    /// Gets the <see cref="IServiceCollection"/> where A2A server services are configured.
    /// </summary>
    IServiceCollection Services { get; }
}