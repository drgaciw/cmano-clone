namespace ProjectAegis.Ai.Host;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

/// <summary>DI composition root for the Wave 0 AI host (fake client only).</summary>
public static class AiHostComposer
{
    /// <summary>
    /// Registers host options, <see cref="FakeChatClient"/> as <see cref="IChatClient"/>,
    /// artifact store, and telemetry. Does not register paid provider SDKs.
    /// </summary>
    public static IServiceCollection AddProjectAegisAiHost(
        this IServiceCollection services,
        string artifactRoot,
        Action<AiHostOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactRoot);

        services.AddOptions<AiHostOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddSingleton<AiHostTelemetry>();
        services.AddSingleton<FakeChatClient>();
        services.AddSingleton<IChatClient>(sp => sp.GetRequiredService<FakeChatClient>());
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AiHostOptions>>().Value;
            return new AtomicArtifactStore(artifactRoot, options);
        });

        return services;
    }
}
