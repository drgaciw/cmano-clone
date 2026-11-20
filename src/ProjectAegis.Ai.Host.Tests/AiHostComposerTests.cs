using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProjectAegis.Ai.Contracts;
using NUnit.Framework;

namespace ProjectAegis.Ai.Host.Tests;

[TestFixture]
public sealed class AiHostComposerTests
{
    [Test]
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

            Assert.That(chat, Is.SameAs(fake));
            Assert.That(store.RootDirectory, Is.EqualTo(root));
            Assert.That(telemetry.Events, Is.Empty);
            Assert.That(options.MaxRequestConcurrency, Is.EqualTo(2));
            Assert.That(options.MaxOutputTokens, Is.EqualTo(128));

            fake.Enqueue(FakeChatScript.Success);
            telemetry.RecordGeneration("fake", "fake-model", GenerationStatus.Succeeded);
            Assert.That(string.Join('|', telemetry.Events), Does.Not.Contain("prompt").IgnoreCase);
            Assert.That(string.Join('|', telemetry.Events), Does.Not.Contain("secret").IgnoreCase);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [TestCase(0)]
    [TestCase(-3)]
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

            Assert.That(options.MaxRequestConcurrency, Is.EqualTo(1));
            Assert.That(options.MaxOutputTokens, Is.EqualTo(64));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [TestCase(0)]
    [TestCase(-7)]
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

            Assert.That(options.MaxRequestConcurrency, Is.EqualTo(4));
            Assert.That(options.MaxOutputTokens, Is.EqualTo(1));
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
