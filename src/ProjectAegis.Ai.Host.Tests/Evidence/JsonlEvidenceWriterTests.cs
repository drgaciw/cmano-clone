using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host.Evidence;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using Xunit;

namespace ProjectAegis.Ai.Host.Tests.Evidence;

public sealed class JsonlEvidenceWriterTests
{
    private readonly JsonlEvidenceWriter _writer = new();

    [Fact]
    public async Task Round_trip_parse_equals_input_document()
    {
        var doc = SampleDocument();
        var root = CreateTempRoot();
        try
        {
            var result = await _writer.WriteAsync(doc, root, "evidence.jsonl");
            Assert.True(result.Published);
            Assert.False(string.IsNullOrWhiteSpace(result.ContentHashSha256));
            Assert.True(result.ByteLength > 0);

            var again = await JsonlEvidenceWriter.ReadRoundTripAsync(result.PublishedPath!);
            Assert.Equal(doc.Header.SchemaVersion, again.Header.SchemaVersion);
            Assert.Equal(doc.Header.BuildIdentity, again.Header.BuildIdentity);
            Assert.Equal(doc.Header.EvidenceIds, again.Header.EvidenceIds);
            Assert.Equal(doc.Entries.Count, again.Entries.Count);
            Assert.Equal(doc.Entries[0].EvidenceId, again.Entries[0].EvidenceId);
            Assert.Equal(doc.Entries[0].Kind, again.Entries[0].Kind);
            Assert.Equal(doc.Entries[0].Attributes!["reason"], again.Entries[0].Attributes!["reason"]);
        }
        finally
        {
            TryDeleteTree(root);
        }
    }

    [Fact]
    public async Task Identical_input_yields_byte_identical_output()
    {
        var doc = SampleDocument();
        var root = CreateTempRoot();
        try
        {
            var a = await _writer.WriteAsync(doc, root, "a.jsonl");
            var b = await _writer.WriteAsync(doc, root, "b.jsonl");
            var bytesA = await File.ReadAllBytesAsync(a.PublishedPath!);
            var bytesB = await File.ReadAllBytesAsync(b.PublishedPath!);
            Assert.Equal(bytesA, bytesB);
            Assert.Equal(a.ContentHashSha256, b.ContentHashSha256);
            Assert.DoesNotContain((byte)'\r', bytesA);
            Assert.Contains((byte)'\n', bytesA);
        }
        finally
        {
            TryDeleteTree(root);
        }
    }

    [Fact]
    public async Task Disabled_export_leaves_no_published_file()
    {
        var root = CreateTempRoot();
        try
        {
            var result = await _writer.WriteAsync(
                SampleDocument(),
                root,
                "evidence.jsonl",
                enabled: false);
            Assert.False(result.Published);
            Assert.Equal("disabled", result.SkipReason);
            Assert.Empty(Directory.GetFiles(root));
        }
        finally
        {
            TryDeleteTree(root);
        }
    }

    [Fact]
    public async Task Delayed_export_still_byte_identical_to_immediate()
    {
        var doc = SampleDocument();
        var root = CreateTempRoot();
        try
        {
            var immediate = await _writer.WriteAsync(doc, root, "immediate.jsonl");
            await Task.Delay(25);
            var delayed = await _writer.WriteAsync(doc, root, "delayed.jsonl");
            Assert.Equal(
                await File.ReadAllBytesAsync(immediate.PublishedPath!),
                await File.ReadAllBytesAsync(delayed.PublishedPath!));
        }
        finally
        {
            TryDeleteTree(root);
        }
    }

    [Fact]
    public async Task Cancellation_mid_export_leaves_no_published_file_and_rerun_succeeds()
    {
        var root = CreateTempRoot();
        try
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                _writer.WriteAsync(SampleDocument(), root, "evidence.jsonl", cancellationToken: cts.Token));

            Assert.False(File.Exists(Path.Combine(root, "evidence.jsonl")));
            Assert.Empty(Directory.GetFiles(root, "*.tmp", SearchOption.AllDirectories));

            var retry = await _writer.WriteAsync(SampleDocument(), root, "evidence.jsonl");
            Assert.True(retry.Published);
            Assert.True(File.Exists(retry.PublishedPath));
        }
        finally
        {
            TryDeleteTree(root);
        }
    }

    [Fact]
    public async Task Failing_oversized_export_leaves_no_published_file()
    {
        var root = CreateTempRoot();
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _writer.WriteAsync(SampleDocument(), root, "evidence.jsonl", maxEvidenceBytes: 16));

            Assert.False(File.Exists(Path.Combine(root, "evidence.jsonl")));
            Assert.Empty(Directory.GetFiles(root, "*.tmp", SearchOption.AllDirectories));
        }
        finally
        {
            TryDeleteTree(root);
        }
    }

    [Fact]
    public async Task Export_enabled_disabled_or_failing_does_not_mutate_order_log_fingerprint()
    {
        var log = new DecisionLog();
        log.Append(OrderLogEntryFactories.FromEngagement(
            new EngagementRecord(
                SequenceId: 1,
                SimTime: 1.0,
                SimTick: 1,
                ShooterTargetId: new TargetId("u1"),
                EngagementId: 9,
                Launched: true,
                VictimTargetId: new TargetId("hostile-1"))));

        var before = log.ComputeFingerprint();
        var projected = new EvidenceProjector().Project(
            log,
            new EvidenceProjectionRequest(
                "build",
                "scenario",
                "cat",
                ViewingSide.Blue,
                DebriefMode: false,
                FingerprintPrefix: "fp"));

        var root = CreateTempRoot();
        try
        {
            await _writer.WriteAsync(projected.Document, root, "on.jsonl", enabled: true);
            Assert.Equal(before, log.ComputeFingerprint());

            await _writer.WriteAsync(projected.Document, root, "off.jsonl", enabled: false);
            Assert.Equal(before, log.ComputeFingerprint());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _writer.WriteAsync(projected.Document, root, "fail.jsonl", maxEvidenceBytes: 8));
            Assert.Equal(before, log.ComputeFingerprint());
        }
        finally
        {
            TryDeleteTree(root);
        }
    }

    [Fact]
    public async Task Peak_memory_delta_recorded_for_small_and_large_logs()
    {
        var root = CreateTempRoot();
        try
        {
            var small = await _writer.WriteAsync(SampleDocument(entryCount: 2), root, "small.jsonl");
            var large = await _writer.WriteAsync(SampleDocument(entryCount: 200), root, "large.jsonl");
            Assert.True(small.Published);
            Assert.True(large.Published);
            Assert.True(large.ByteLength > small.ByteLength);
            // Peak delta is observational (may be 0 under GC timing); field must be present and non-negative.
            Assert.True(small.PeakWorkingSetBytesDelta >= 0);
            Assert.True(large.PeakWorkingSetBytesDelta >= 0);
        }
        finally
        {
            TryDeleteTree(root);
        }
    }

    [Fact]
    public async Task Envelope_records_jsonl_content_hash_via_atomic_store()
    {
        var root = CreateTempRoot();
        try
        {
            var doc = SampleDocument();
            var write = await _writer.WriteAsync(doc, root, "evidence.jsonl");
            var store = new AtomicArtifactStore(root, new AiHostOptions());
            await JsonlEvidenceEnvelopePublisher.PublishAsync(
                store,
                "evidence.envelope.json",
                doc,
                write);

            Assert.True(store.Exists("evidence.envelope.json"));
            var text = await File.ReadAllTextAsync(Path.Combine(root, "evidence.envelope.json"));
            Assert.Contains(write.ContentHashSha256!, text, StringComparison.Ordinal);
        }
        finally
        {
            TryDeleteTree(root);
        }
    }

    [Fact]
    public void Source_fence_Jsonl_files_do_not_reference_forbidden_symbols()
    {
        var hostRoot = FindHostEvidenceRoot();
        foreach (var path in Directory.EnumerateFiles(hostRoot, "Jsonl*.cs")
                     .Concat(Directory.EnumerateFiles(hostRoot, "EvidenceJsonlJsonContext.cs")))
        {
            var code = string.Join(
                '\n',
                File.ReadLines(path).Where(line =>
                {
                    var trimmed = line.TrimStart();
                    return !trimmed.StartsWith("//", StringComparison.Ordinal)
                        && !trimmed.StartsWith("///", StringComparison.Ordinal)
                        && !trimmed.StartsWith("*", StringComparison.Ordinal);
                }));

            Assert.DoesNotContain("OnScenarioFinalized", code);
            Assert.DoesNotContain("DelegationBridge", code);
            Assert.DoesNotContain(".Append(", code);
        }
    }

    private static EvidenceDocument SampleDocument(int entryCount = 2)
    {
        var ids = new List<string>(entryCount);
        var entries = new List<EvidenceEntry>(entryCount);
        for (var i = 1; i <= entryCount; i++)
        {
            var id = "fp:" + i;
            ids.Add(id);
            entries.Add(new EvidenceEntry(
                EvidenceId: id,
                SequenceId: (ulong)i,
                Kind: i % 2 == 0 ? EvidenceEntryKind.Engagement : EvidenceEntryKind.PolicyDenial,
                SimTime: i,
                ViewingSide: ViewingSide.Blue,
                UnitId: "u1",
                Summary: "row-" + i,
                Attributes: new Dictionary<string, string> { ["reason"] = "roe-" + i }));
        }

        return new EvidenceDocument(
            new EvidenceHeader(
                AiContractSchema.Version,
                "build-1",
                "scenario-a",
                "cat-1",
                "digestfp",
                ViewingSide.Blue,
                DebriefMode: false,
                EvidenceIds: ids),
            entries);
    }

    private static string CreateTempRoot() =>
        Directory.CreateDirectory(
                Path.Combine(Path.GetTempPath(), "aegis-jsonl-" + Guid.NewGuid().ToString("N")))
            .FullName;

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

    private static string FindHostEvidenceRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "ProjectAegis.Ai.Host", "Evidence");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("ProjectAegis.Ai.Host/Evidence not found");
    }
}
