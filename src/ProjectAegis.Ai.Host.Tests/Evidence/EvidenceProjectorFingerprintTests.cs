using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host.Evidence;
using ProjectAegis.Delegation.AfterAction;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using NUnit.Framework;

namespace ProjectAegis.Ai.Host.Tests.Evidence;

[TestFixture]
public sealed class EvidenceProjectorFingerprintTests
{
    [Test]
    public void Project_does_not_mutate_order_log_fingerprint()
    {
        var log = new DecisionLog();
        log.Append(OrderLogEntryFactories.FromControllerChange(
            new ControllerChangeRecord(
                SequenceId: 1,
                SimTime: 0.0,
                TargetId: new TargetId("u1"),
                PreviousKind: "Human",
                NewKind: "Agent",
                AgentId: new AgentId("agent-blue-1"))));
        log.Append(OrderLogEntryFactories.FromEngagement(
            new EngagementRecord(
                SequenceId: 2,
                SimTime: 12.0,
                SimTick: 12,
                ShooterTargetId: new TargetId("u1"),
                EngagementId: 1001,
                Launched: true,
                VictimTargetId: new TargetId("hostile-1"),
                WeaponFamilyId: "asm-fixture",
                SalvoSize: 2)));

        var before = log.ComputeFingerprint();
        Assert.That(string.IsNullOrWhiteSpace(before), Is.False);

        var projector = new EvidenceProjector();
        var result = projector.Project(
            log,
            new EvidenceProjectionRequest(
                BuildIdentity: "fp-test",
                ScenarioId: "fingerprint",
                CatalogRevision: "rev",
                ViewingSide: ViewingSide.Blue,
                DebriefMode: false,
                FingerprintPrefix: "fp"));

        var after = log.ComputeFingerprint();
        Assert.That(after, Is.EqualTo(before));
        Assert.That(result.Document.Header.OrderLogFingerprint, Is.EqualTo(EvidenceProjector.DigestOrderLogFingerprint(before)));
        Assert.That(result.Document.Header.OrderLogFingerprint, Does.Not.Contain("hostile-1"));
        Assert.That(result.Document.Header.OrderLogFingerprint, Does.Not.Contain('\n'));
        Assert.That(result.Document.Entries.Count, Is.EqualTo(2));
        Assert.That(result.Document.Header.EvidenceIds, Is.EqualTo(new[] { "fp:1", "fp:2" }));
        Assert.That(result.Quantitative.EngagementCount, Is.EqualTo(1));
        Assert.That(result.Quantitative.ControllerChangeCount, Is.EqualTo(1));
    }

    [Test]
    public void Header_fingerprint_digest_does_not_embed_hidden_row_plaintext()
    {
        var log = new DecisionLog();
        log.Append(OrderLogEntryFactories.FromEngagement(
            new EngagementRecord(
                SequenceId: 1,
                SimTime: 8.0,
                SimTick: 8,
                ShooterTargetId: new TargetId("u1"),
                EngagementId: 6001,
                Launched: true,
                VictimTargetId: new TargetId("hostile-1"),
                SalvoSize: 1)));
        log.Append(OrderLogEntryFactories.FromEngagement(
            new EngagementRecord(
                SequenceId: 2,
                SimTime: 8.5,
                SimTick: 8,
                ShooterTargetId: new TargetId("hostile-1"),
                EngagementId: 6002,
                Launched: true,
                VictimTargetId: new TargetId("u1"),
                SalvoSize: 4)));

        var annotations = new Dictionary<ulong, EvidenceSourceAnnotation>
        {
            [1] = new(ViewingSide.Blue, Array.Empty<ViewingSide>()),
            [2] = new(ViewingSide.Red, new[] { ViewingSide.Blue }),
        };

        var raw = log.ComputeFingerprint();
        Assert.That(raw, Does.Contain("6002"));

        var result = new EvidenceProjector().Project(
            log,
            new EvidenceProjectionRequest(
                "build",
                "two-side-fp",
                "rev",
                ViewingSide.Blue,
                DebriefMode: false,
                FingerprintPrefix: "fp"),
            annotations);

        Assert.That(result.Document.Entries, Has.Count.EqualTo(1));
        Assert.That(result.Document.Header.OrderLogFingerprint, Is.EqualTo(EvidenceProjector.DigestOrderLogFingerprint(raw)));
        Assert.That(result.Document.Header.OrderLogFingerprint, Does.Not.Contain("6002"));
        Assert.That(result.Document.Header.OrderLogFingerprint, Does.Not.Contain("hostile-1"));
        Assert.That(result.Document.Header.OrderLogFingerprint.Length, Is.EqualTo(64));
    }

    [Test]
    public void Non_debrief_does_not_attach_full_log_aar_attributes_to_visible_entries()
    {
        var log = new StubReadOnlyOrderLog(
            new[]
            {
                new OrderLogEntry(
                    1,
                    OrderLogEntryKind.Engagement,
                    1.0,
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["shooterUnitId"] = "u1",
                        ["summary"] = "blue-shot",
                    }),
            },
            "aar-leak");

        var aar = new AgentPerformanceSection(
            new[]
            {
                new AgentPerformanceRow("agent-red-1", IntentCount: 3, PolicyDenialCount: 0, OverrideCount: 0),
                new AgentPerformanceRow("agent-blue-1", IntentCount: 1, PolicyDenialCount: 0, OverrideCount: 0),
            });

        var blue = new EvidenceProjector().Project(
            log,
            new EvidenceProjectionRequest(
                "build",
                "aar-side",
                "rev",
                ViewingSide.Blue,
                DebriefMode: false,
                FingerprintPrefix: "fp",
                AarSection: aar));

        Assert.That(blue.AarSection!.Rows, Is.Empty);
        Assert.That(blue.Document.Entries.SelectMany(e => e.Attributes?.Keys ?? Array.Empty<string>()).Any(key => key.StartsWith("aar.", StringComparison.Ordinal)), Is.False);

        var debrief = new EvidenceProjector().Project(
            log,
            new EvidenceProjectionRequest(
                "build",
                "aar-side",
                "rev",
                ViewingSide.Blue,
                DebriefMode: true,
                FingerprintPrefix: "fp",
                AarSection: aar));

        Assert.That(debrief.AarSection!.Rows.Count, Is.EqualTo(2));
        Assert.That(debrief.Document.Entries.SelectMany(e => e.Attributes?.Keys ?? Array.Empty<string>()).Any(key => key.StartsWith("aar.agent-red-1.", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void Typed_contact_player_damage_payloads_project_from_DecisionLog()
    {
        var log = new DecisionLog();
        log.Append(OrderLogEntryFactories.FromContactChange(
            new ContactChangeRecord(
                SequenceId: 1,
                SimTime: 1.0,
                SimTick: 1,
                ObserverId: "u1",
                ContactId: "c-1",
                TargetId: "hostile-1",
                PreviousState: "Unknown",
                NewState: "Detected")));
        log.Append(OrderLogEntryFactories.FromPlayerOrder(
            new PlayerOrderRecord(
                SequenceId: 2,
                SimTime: 2.0,
                SimTick: 2,
                UnitId: new TargetId("u1"),
                Kind: OrderKind.Hold)));
        log.Append(OrderLogEntryFactories.FromPlatformDamageChange(
            new PlatformDamageChangeRecord(
                SequenceId: 3,
                SimTime: 3.0,
                SimTick: 3,
                UnitId: new TargetId("hostile-1"),
                PreviousHpPct: 1.0,
                NewHpPct: 0.4,
                ReasonCode: "Hit",
                DamageLevel: 2)));

        var before = log.ComputeFingerprint();
        var result = new EvidenceProjector().Project(
            log,
            new EvidenceProjectionRequest(
                "build",
                "typed",
                "rev",
                ViewingSide.Blue,
                DebriefMode: false,
                FingerprintPrefix: "fp"));

        Assert.That(log.ComputeFingerprint(), Is.EqualTo(before));
        Assert.That(result.Document.Entries.Count, Is.EqualTo(3));

        var contact = result.Document.Entries[0];
        Assert.That(contact.Kind, Is.EqualTo(EvidenceEntryKind.ContactChange));
        Assert.That(contact.UnitId, Is.EqualTo("hostile-1"));
        Assert.That(contact.Summary, Is.EqualTo("Unknown->Detected"));
        Assert.That(contact.Attributes!["contactId"], Is.EqualTo("c-1"));

        var player = result.Document.Entries[1];
        Assert.That(player.Kind, Is.EqualTo(EvidenceEntryKind.PlayerOrder));
        Assert.That(player.UnitId, Is.EqualTo("u1"));
        Assert.That(player.Summary, Is.EqualTo("Hold"));
        Assert.That(player.Attributes!["source"], Is.EqualTo("player"));

        var damage = result.Document.Entries[2];
        Assert.That(damage.Kind, Is.EqualTo(EvidenceEntryKind.PlatformDamageChange));
        Assert.That(damage.UnitId, Is.EqualTo("hostile-1"));
        Assert.That(damage.Summary, Is.EqualTo("Hit"));
        Assert.That(damage.Attributes!["newHpPct"], Is.EqualTo("0.4"));
    }

    [Test]
    public void Source_fence_Evidence_files_do_not_reference_forbidden_symbols()
    {
        var hostRoot = FindHostEvidenceRoot();
        // Projector Surface only — JSONL writer (DRG-743) owns SerializeAsyncEnumerable.
        foreach (var path in Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(path);
            if (name.StartsWith("Jsonl", StringComparison.Ordinal)
                || name.StartsWith("EvidenceJsonl", StringComparison.Ordinal))
            {
                continue;
            }

            var code = string.Join(
                '\n',
                File.ReadLines(path).Where(line =>
                {
                    var trimmed = line.TrimStart();
                    return !trimmed.StartsWith("//", StringComparison.Ordinal)
                        && !trimmed.StartsWith("///", StringComparison.Ordinal)
                        && !trimmed.StartsWith("*", StringComparison.Ordinal);
                }));

            Assert.That(code, Does.Not.Contain("IOrderLog.Append"));
            Assert.That(code, Does.Not.Contain(".Append("));
            Assert.That(code, Does.Not.Contain("DelegationBridge"));
            Assert.That(code, Does.Not.Contain("SimulationSession"));
            Assert.That(code, Does.Not.Contain("UnityEngine"));
            Assert.That(code, Does.Not.Contain("OnScenarioFinalized"));
            Assert.That(code, Does.Not.Contain("SerializeAsyncEnumerable"));
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
