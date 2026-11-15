using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ProjectAegis.Ai.Host;

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

        // Clamp and read host limits so option getters are exercised on the DI path.
        services.PostConfigure<AiHostOptions>(options =>
        {
            if (options.MaxRequestConcurrency < 1)
            {
                options.MaxRequestConcurrency = 1;
            }

            if (options.MaxOutputTokens < 1)
            {
                options.MaxOutputTokens = 1;
            }
        });

        services.AddSingleton<AiHostTelemetry>();
        services.AddSingleton<FakeChatClient>();
        services.AddSingleton<IChatClient>(sp => sp.GetRequiredService<FakeChatClient>());
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AiHostOptions>>().Value;
            _ = options.MaxRequestConcurrency;
            _ = options.MaxOutputTokens;
            return new AtomicArtifactStore(artifactRoot, options);
        });

        return services;
    }
}
