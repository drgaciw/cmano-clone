namespace ProjectAegis.Ai.Host.Tests;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ProjectAegis.Ai.Host;
using Xunit;

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

            Assert.Same(fake, chat);
            Assert.Equal(root, store.RootDirectory);
            Assert.Empty(telemetry.Events);

            fake.Enqueue(FakeChatScript.Success);
            telemetry.RecordGeneration("fake", "fake-model", ProjectAegis.Ai.Contracts.GenerationStatus.Succeeded);
            Assert.DoesNotContain("prompt", string.Join('|', telemetry.Events), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("secret", string.Join('|', telemetry.Events), StringComparison.OrdinalIgnoreCase);
        }
        finally
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
}
