using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host;
using Xunit;

namespace ProjectAegis.Ai.Host.Tests;

public sealed class AtomicArtifactStoreTests
{
    [Fact]
    public async Task Publish_writes_envelope_atomically()
    {
        var root = CreateTempRoot();
        try
        {
            var store = new AtomicArtifactStore(root, new AiHostOptions());
            var envelope = SampleEnvelope(GenerationStatus.Succeeded);

            await store.PublishAsync("artifact.json", envelope);

            Assert.True(store.Exists("artifact.json"));
            Assert.Empty(Directory.GetFiles(root, "*.tmp"));
            var text = await File.ReadAllTextAsync(Path.Combine(root, "artifact.json"));
            Assert.Contains("\"schemaVersion\":1", text, StringComparison.Ordinal);
            Assert.Contains("\"providerId\":\"fake\"", text, StringComparison.Ordinal);
        }
        finally
        {
            TryDeleteTree(root);
        }
    }

    [Fact]
    public async Task Cancellation_mid_write_leaves_no_published_artifact_and_rerun_succeeds()
    {
        var root = CreateTempRoot();
        try
        {
            var store = new AtomicArtifactStore(root, new AiHostOptions());
            var envelope = SampleEnvelope(GenerationStatus.Cancelled);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                store.PublishAsync("artifact.json", envelope, cts.Token));

            Assert.False(store.Exists("artifact.json"));
            Assert.Empty(Directory.GetFiles(root, "*.tmp", SearchOption.AllDirectories));

            var retry = SampleEnvelope(GenerationStatus.Succeeded);
            await store.PublishAsync("artifact.json", retry);
            Assert.True(store.Exists("artifact.json"));
        }
        finally
        {
            TryDeleteTree(root);
        }
    }

    [Fact]
    public async Task Oversized_payload_does_not_publish()
    {
        var root = CreateTempRoot();
        try
        {
            var store = new AtomicArtifactStore(root, new AiHostOptions { MaxEvidenceBytes = 32 });
            var envelope = SampleEnvelope(GenerationStatus.Succeeded);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                store.PublishAsync("artifact.json", envelope));

            Assert.False(store.Exists("artifact.json"));
        }
        finally
        {
            TryDeleteTree(root);
        }
    }

    private static ArtifactEnvelope SampleEnvelope(GenerationStatus status) =>
        new(
            SchemaVersion: AiContractSchema.Version,
            BuildIdentity: "test-build",
            ScenarioId: "scenario-1",
            CatalogRevision: "cat-1",
            OrderLogFingerprint: "fpdeadbeef",
            ProviderId: "fake",
            ModelId: "fake-model",
            PromptVersion: "pv1",
            EvidenceIds: [EvidenceId.From("aifix-test", 1)],
            ViewingSide: ViewingSide.Blue,
            DebriefMode: false,
            Status: status,
            ContentHash: "content-hash");

    private static string CreateTempRoot() =>
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "aegis-ai-host-" + Guid.NewGuid().ToString("N"))).FullName;

    private static void TryDeleteTree(string root)
    {
        try
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
        catch (IOException)
        {
            // best-effort
        }
    }
}
