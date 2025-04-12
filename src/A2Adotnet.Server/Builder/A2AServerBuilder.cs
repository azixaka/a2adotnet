using Microsoft.Extensions.DependencyInjection;

namespace A2Adotnet.Server.Builder;

/// <summary>
/// Default implementation of <see cref="IA2AServerBuilder"/>.
/// </summary>
internal class A2AServerBuilder : IA2AServerBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="A2AServerBuilder"/> class.
    /// </summary>
    /// <param name="services">The services being configured.</param>
    public A2AServerBuilder(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <inheritdoc />
    public IServiceCollection Services { get; }
}