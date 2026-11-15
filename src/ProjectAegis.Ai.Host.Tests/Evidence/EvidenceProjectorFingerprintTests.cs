using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host.Evidence;
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
        Assert.Equal(before, result.Document.Header.OrderLogFingerprint);
        Assert.Equal(2, result.Document.Entries.Count);
        Assert.Equal(new[] { "fp:1", "fp:2" }, result.Document.Header.EvidenceIds);
        Assert.Equal(1, result.Quantitative.EngagementCount);
        Assert.Equal(1, result.Quantitative.ControllerChangeCount);
    }

    [Fact]
    public void Source_fence_Evidence_files_do_not_reference_forbidden_symbols()
    {
        var hostRoot = FindHostEvidenceRoot();
        foreach (var path in Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories))
        {
            // Strip doc/line comments so fence language in XML docs does not false-positive.
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
