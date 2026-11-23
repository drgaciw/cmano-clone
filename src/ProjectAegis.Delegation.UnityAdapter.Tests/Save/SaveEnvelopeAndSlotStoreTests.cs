namespace ProjectAegis.Delegation.UnityAdapter.Tests.Save;

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Input;
using ProjectAegis.Delegation.UnityAdapter.Save;

[TestFixture]
public sealed class SaveEnvelopeAndSlotStoreTests
{
    [Test]
    public void Rejects_truncated_stream_before_candidate_construction()
    {
        var validBytes = SaveEnvelopeSerializer.SerializeToUtf8Bytes(CreateValidEnvelope());
        var truncated = validBytes.Take(validBytes.Length / 2).ToArray();

        var result = SaveEnvelopeSerializer.TryDeserializeAndVerify(truncated);
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo(SaveFailureCode.TruncatedOrMalformedJson));
    }

    [Test]
    public void Rejects_unknown_schema_version_before_candidate_construction()
    {
        var validBytes = SaveEnvelopeSerializer.SerializeToUtf8Bytes(CreateValidEnvelope());
        var root = JsonNode.Parse(validBytes)!.AsObject();
        root["identity"]!.AsObject()["schemaVersion"] = 999;

        var tamperedBytes = Encoding.UTF8.GetBytes(root.ToJsonString());
        var result = SaveEnvelopeSerializer.TryDeserializeAndVerify(tamperedBytes);
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo(SaveFailureCode.UnknownSchemaVersion));
    }

    [Test]
    public void Rejects_missing_observed_or_pipeline_clock_or_sequence_id()
    {
        var validBytes = SaveEnvelopeSerializer.SerializeToUtf8Bytes(CreateValidEnvelope());

        foreach (var fieldToRemove in new[] { "observedTick", "pipelineTick", "lastSequenceId" })
        {
            var root = JsonNode.Parse(validBytes)!.AsObject();
            root["position"]!.AsObject().Remove(fieldToRemove);

            var result = SaveEnvelopeSerializer.TryDeserializeAndVerify(Encoding.UTF8.GetBytes(root.ToJsonString()));
            Assert.That(result.Succeeded, Is.False, fieldToRemove);
            Assert.That(result.FailureCode, Is.EqualTo(SaveFailureCode.MissingRequiredField), fieldToRemove);
        }
    }

    [Test]
    public void Rejects_extra_multiplayer_or_network_fields()
    {
        var validBytes = SaveEnvelopeSerializer.SerializeToUtf8Bytes(CreateValidEnvelope());
        foreach (var forbidden in new[] { "peer", "peers", "networkMode", "hotSeat", "slotSide", "lockstep" })
        {
            var root = JsonNode.Parse(validBytes)!.AsObject();
            root[forbidden] = "invalid-in-h6";

            var result = SaveEnvelopeSerializer.TryDeserializeAndVerify(Encoding.UTF8.GetBytes(root.ToJsonString()));
            Assert.That(result.Succeeded, Is.False, forbidden);
            Assert.That(result.FailureCode, Is.EqualTo(SaveFailureCode.UnsupportedNetworkField), forbidden);
        }
    }

    [Test]
    public void Rejects_tampered_envelope_hash_and_tampered_journal_digest()
    {
        var envelope = CreateValidEnvelope();
        var validBytes = SaveEnvelopeSerializer.SerializeToUtf8Bytes(envelope);

        // 1. Tamper EnvelopeHashSha256 only
        {
            var root = JsonNode.Parse(validBytes)!.AsObject();
            root["envelopeHashSha256"] = new string('0', 64);
            var res = SaveEnvelopeSerializer.TryDeserializeAndVerify(Encoding.UTF8.GetBytes(root.ToJsonString()));
            Assert.That(res.Succeeded, Is.False);
            Assert.That(res.FailureCode, Is.EqualTo(SaveFailureCode.IntegrityMismatch));
        }

        // 2. Tamper JournalDigest while keeping OrderLogFingerprintSha256 and LastWorldHash untouched
        {
            var root = JsonNode.Parse(validBytes)!.AsObject();
            root["checksums"]!.AsObject()["journalDigest"] = new string('f', 64);
            var res = SaveEnvelopeSerializer.TryDeserializeAndVerify(Encoding.UTF8.GetBytes(root.ToJsonString()));
            Assert.That(res.Succeeded, Is.False);
            Assert.That(res.FailureCode, Is.EqualTo(SaveFailureCode.IntegrityMismatch));
        }
    }

    [Test]
    public void Rejects_oversized_payload_and_excessive_json_depth()
    {
        var oversized = new byte[SaveEnvelopeBounds.MaxPayloadBytes + 1];
        Array.Fill(oversized, (byte)' ');
        var resSize = SaveEnvelopeSerializer.TryDeserializeAndVerify(oversized);
        Assert.That(resSize.Succeeded, Is.False);
        Assert.That(resSize.FailureCode, Is.EqualTo(SaveFailureCode.PayloadTooLarge));

        var deep = new StringBuilder();
        for (var i = 0; i < SaveEnvelopeBounds.MaxJsonDepth + 5; i++) deep.Append("{\"a\":");
        deep.Append("1");
        for (var i = 0; i < SaveEnvelopeBounds.MaxJsonDepth + 5; i++) deep.Append('}');
        var resDepth = SaveEnvelopeSerializer.TryDeserializeAndVerify(Encoding.UTF8.GetBytes(deep.ToString()));
        Assert.That(resDepth.Succeeded, Is.False);
        Assert.That(resDepth.FailureCode, Is.EqualTo(SaveFailureCode.DepthLimitExceeded).Or.EqualTo(SaveFailureCode.TruncatedOrMalformedJson));
    }

    [Test]
    public void SaveSlotStore_supports_three_manual_slots_and_one_rolling_autosave_independently()
    {
        var tempDir = CreateTempDir();
        try
        {
            var store = new SaveSlotStore(tempDir);
            Assert.That(store.IsValidSlotId("manual-1"), Is.True);
            Assert.That(store.IsValidSlotId("manual-2"), Is.True);
            Assert.That(store.IsValidSlotId("manual-3"), Is.True);
            Assert.That(store.IsValidSlotId("autosave"), Is.True);
            Assert.That(store.IsValidSlotId("manual-4"), Is.False);
            Assert.That(store.IsValidSlotId("../escape"), Is.False);

            var e1 = CreateValidEnvelope(seed: 101);
            var e2 = CreateValidEnvelope(seed: 102);
            var e3 = CreateValidEnvelope(seed: 103);
            var auto1 = CreateValidEnvelope(seed: 201);
            var auto2 = CreateValidEnvelope(seed: 202);

            Assert.That(store.Save("manual-1", e1).Succeeded, Is.True);
            Assert.That(store.Save("manual-2", e2).Succeeded, Is.True);
            Assert.That(store.Save("manual-3", e3).Succeeded, Is.True);
            Assert.That(store.Save("autosave", auto1).Succeeded, Is.True);
            Assert.That(store.Save("autosave", auto2).Succeeded, Is.True);

            Assert.That(store.Load("manual-1").Value!.Identity.GlobalSeed, Is.EqualTo(101));
            Assert.That(store.Load("manual-2").Value!.Identity.GlobalSeed, Is.EqualTo(102));
            Assert.That(store.Load("manual-3").Value!.Identity.GlobalSeed, Is.EqualTo(103));
            Assert.That(store.Load("autosave").Value!.Identity.GlobalSeed, Is.EqualTo(202));
            Assert.That(Directory.GetFiles(tempDir, "*.aegis-save.json"), Has.Length.EqualTo(4));
        }
        finally
        {
            DeleteTempDir(tempDir);
        }
    }

    [Test]
    public void SaveSlotStore_preserves_prior_slot_contents_on_write_failure_or_unjournaled_mutation()
    {
        var tempDir = CreateTempDir();
        try
        {
            var store = new SaveSlotStore(tempDir);
            var good = CreateValidEnvelope(seed: 777);
            Assert.That(store.Save("manual-1", good).Succeeded, Is.True);
            var slotPath = store.TryGetSlotPath("manual-1")!;
            var beforeBytes = File.ReadAllBytes(slotPath);

            // 1. Pre-write unjournaled mutation aborts before writing
            var replacement = CreateValidEnvelope(seed: 888);
            var unjournaledResult = store.Save(
                "manual-1",
                replacement,
                preWriteGuard: () => (false, "DecisionLog mutated outside facade."));
            Assert.That(unjournaledResult.Succeeded, Is.False);
            Assert.That(unjournaledResult.FailureCode, Is.EqualTo(SaveFailureCode.UnjournaledMutationDetected));
            Assert.That(File.ReadAllBytes(slotPath), Is.EqualTo(beforeBytes));

            // 2. Simulated I/O failure mid-write preserves prior slot bytes & cleans temp file
            var ioFailResult = store.Save(
                "manual-1",
                replacement,
                faultInjector: _ => throw new IOException("Simulated disk full."));
            Assert.That(ioFailResult.Succeeded, Is.False);
            Assert.That(ioFailResult.FailureCode, Is.EqualTo(SaveFailureCode.WriteFailed));
            Assert.That(File.ReadAllBytes(slotPath), Is.EqualTo(beforeBytes));
            Assert.That(Directory.GetFiles(tempDir, "*.tmp"), Is.Empty);
        }
        finally
        {
            DeleteTempDir(tempDir);
        }
    }

    [Test]
    public void SaveSlotStore_load_does_not_delete_or_mutate_slot()
    {
        var tempDir = CreateTempDir();
        try
        {
            var store = new SaveSlotStore(tempDir);
            var good = CreateValidEnvelope(seed: 42);
            Assert.That(store.Save("manual-2", good).Succeeded, Is.True);
            var slotPath = store.TryGetSlotPath("manual-2")!;
            var beforeBytes = File.ReadAllBytes(slotPath);

            var loaded1 = store.Load("manual-2");
            var loaded2 = store.Load("manual-2");
            Assert.That(loaded1.Succeeded, Is.True);
            Assert.That(loaded2.Succeeded, Is.True);
            Assert.That(File.Exists(slotPath), Is.True);
            Assert.That(File.ReadAllBytes(slotPath), Is.EqualTo(beforeBytes));
        }
        finally
        {
            DeleteTempDir(tempDir);
        }
    }

    private static SaveEnvelope CreateValidEnvelope(int seed = 42)
    {
        var rows = new[]
        {
            new SaveJournalRow(
                SequenceOrder: 0,
                ObservedTick: 2,
                PipelineTick: 2,
                CommandKind: InputCommandInventory.PauseSimId,
                Arguments: new SaveJournalArguments(SimTime: 2.0)),
        };

        var identity = new SaveIdentityBlock(
            SchemaVersion: SaveEnvelope.CurrentSchemaVersion,
            BuildId: "test-build-1.0",
            ScenarioId: "baltic-patrol",
            ScenarioContentHash: new string('a', 64),
            CatalogSnapshotId: "snap-1",
            WholeCatalogHash: new string('b', 64),
            PolicyHashes: [new SavePolicyHashEntry("baltic-patrol", new string('c', 64))],
            GlobalSeed: seed);

        var position = new SavePositionBlock(ObservedTick: 4, PipelineTick: 4, LastSequenceId: 12);
        var checkpoints = new[]
        {
            new SaveCheckpointRow(SimTick: 4, WorldHash: 17144800277401907079UL, LogFingerprint: "raw-fp", LastSequenceId: 12),
        };

        return SaveEnvelopeSerializer.CreateSigned(
            identity,
            position,
            rows,
            orderLogFingerprintSha256: new string('d', 64),
            lastWorldHash: 987654321UL,
            combinedWorldHash: 17144800277401907079UL,
            checkpointRows: checkpoints);
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "aegis-save-slot-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteTempDir(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
