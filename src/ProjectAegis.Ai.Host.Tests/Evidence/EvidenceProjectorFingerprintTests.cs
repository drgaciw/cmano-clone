using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host.Evidence;
using ProjectAegis.Delegation.AfterAction;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using Xunit;

namespace ProjectAegis.Ai.Host.Tests.Evidence;

public sealed class EvidenceProjectorFingerprintTests
{
    [Fact]
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
        Assert.False(string.IsNullOrWhiteSpace(before));

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
        Assert.Equal(before, after);
        Assert.Equal(
            EvidenceProjector.DigestOrderLogFingerprint(before),
            result.Document.Header.OrderLogFingerprint);
        Assert.DoesNotContain("hostile-1", result.Document.Header.OrderLogFingerprint);
        Assert.DoesNotContain('\n', result.Document.Header.OrderLogFingerprint);
        Assert.Equal(2, result.Document.Entries.Count);
        Assert.Equal(new[] { "fp:1", "fp:2" }, result.Document.Header.EvidenceIds);
        Assert.Equal(1, result.Quantitative.EngagementCount);
        Assert.Equal(1, result.Quantitative.ControllerChangeCount);
    }

    [Fact]
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
        Assert.Contains("6002", raw, StringComparison.Ordinal);

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

        Assert.Single(result.Document.Entries);
        Assert.Equal(EvidenceProjector.DigestOrderLogFingerprint(raw), result.Document.Header.OrderLogFingerprint);
        Assert.DoesNotContain("6002", result.Document.Header.OrderLogFingerprint);
        Assert.DoesNotContain("hostile-1", result.Document.Header.OrderLogFingerprint);
        Assert.Equal(64, result.Document.Header.OrderLogFingerprint.Length);
    }

    [Fact]
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

        Assert.Empty(blue.AarSection!.Rows);
        Assert.DoesNotContain(
            blue.Document.Entries.SelectMany(e => e.Attributes?.Keys ?? Array.Empty<string>()),
            key => key.StartsWith("aar.", StringComparison.Ordinal));

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

        Assert.Equal(2, debrief.AarSection!.Rows.Count);
        Assert.Contains(
            debrief.Document.Entries.SelectMany(e => e.Attributes?.Keys ?? Array.Empty<string>()),
            key => key.StartsWith("aar.agent-red-1.", StringComparison.Ordinal));
    }

    [Fact]
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

        Assert.Equal(before, log.ComputeFingerprint());
        Assert.Equal(3, result.Document.Entries.Count);

        var contact = result.Document.Entries[0];
        Assert.Equal(EvidenceEntryKind.ContactChange, contact.Kind);
        Assert.Equal("hostile-1", contact.UnitId);
        Assert.Equal("Unknown->Detected", contact.Summary);
        Assert.Equal("c-1", contact.Attributes!["contactId"]);

        var player = result.Document.Entries[1];
        Assert.Equal(EvidenceEntryKind.PlayerOrder, player.Kind);
        Assert.Equal("u1", player.UnitId);
        Assert.Equal("Hold", player.Summary);
        Assert.Equal("player", player.Attributes!["source"]);

        var damage = result.Document.Entries[2];
        Assert.Equal(EvidenceEntryKind.PlatformDamageChange, damage.Kind);
        Assert.Equal("hostile-1", damage.UnitId);
        Assert.Equal("Hit", damage.Summary);
        Assert.Equal("0.4", damage.Attributes!["newHpPct"]);
    }

    [Fact]
    public void Source_fence_Evidence_files_do_not_reference_forbidden_symbols()
    {
        var hostRoot = FindHostEvidenceRoot();
        foreach (var path in Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories))
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

            Assert.DoesNotContain("IOrderLog.Append", code);
            Assert.DoesNotContain(".Append(", code);
            Assert.DoesNotContain("DelegationBridge", code);
            Assert.DoesNotContain("SimulationSession", code);
            Assert.DoesNotContain("UnityEngine", code);
            Assert.DoesNotContain("OnScenarioFinalized", code);
            Assert.DoesNotContain("SerializeAsyncEnumerable", code);
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
