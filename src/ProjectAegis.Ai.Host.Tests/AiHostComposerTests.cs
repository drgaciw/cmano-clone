using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProjectAegis.Ai.Contracts;
using Xunit;

namespace ProjectAegis.Ai.Host.Tests;

public sealed class AiHostComposerTests
{
    [Fact]
    public void Composition_root_registers_fake_client_without_network()
    {
        var root = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), "aegis-ai-di-" + Guid.NewGuid().ToString("N"))).FullName;
        try
        {
            var services = new ServiceCollection();
            services.AddProjectAegisAiHost(root, o =>
            {
                o.MaxRequestConcurrency = 2;
                o.MaxOutputTokens = 128;
            });

            using var provider = services.BuildServiceProvider();
            var fake = provider.GetRequiredService<FakeChatClient>();
            var chat = provider.GetRequiredService<IChatClient>();
            var store = provider.GetRequiredService<AtomicArtifactStore>();
            var telemetry = provider.GetRequiredService<AiHostTelemetry>();
            var options = provider.GetRequiredService<IOptions<AiHostOptions>>().Value;

            Assert.Same(fake, chat);
            Assert.Equal(root, store.RootDirectory);
            Assert.Empty(telemetry.Events);
            Assert.Equal(2, options.MaxRequestConcurrency);
            Assert.Equal(128, options.MaxOutputTokens);

            fake.Enqueue(FakeChatScript.Success);
            telemetry.RecordGeneration("fake", "fake-model", GenerationStatus.Succeeded);
            Assert.DoesNotContain("prompt", string.Join('|', telemetry.Events), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("secret", string.Join('|', telemetry.Events), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Composition_clamps_invalid_MaxRequestConcurrency_to_one_and_preserves_tokens(int invalidConcurrency)
    {
        var root = CreateTempRoot();
        try
        {
            var services = new ServiceCollection();
            services.AddProjectAegisAiHost(root, o =>
            {
                o.MaxRequestConcurrency = invalidConcurrency;
                o.MaxOutputTokens = 64;
            });

            using var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<AiHostOptions>>().Value;

            Assert.Equal(1, options.MaxRequestConcurrency);
            Assert.Equal(64, options.MaxOutputTokens);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-7)]
    public void Composition_clamps_invalid_MaxOutputTokens_to_one_and_preserves_concurrency(int invalidTokens)
    {
        var root = CreateTempRoot();
        try
        {
            var services = new ServiceCollection();
            services.AddProjectAegisAiHost(root, o =>
            {
                o.MaxRequestConcurrency = 4;
                o.MaxOutputTokens = invalidTokens;
            });

            using var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<AiHostOptions>>().Value;

            Assert.Equal(4, options.MaxRequestConcurrency);
            Assert.Equal(1, options.MaxOutputTokens);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    private static string CreateTempRoot() =>
        Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), "aegis-ai-di-" + Guid.NewGuid().ToString("N"))).FullName;

    private static void DeleteTempRoot(string root)
    {
        try
        {
            Directory.Delete(root, recursive: true);
        }
        catch (IOException)
        {
            // best-effort
        }
    }
}
