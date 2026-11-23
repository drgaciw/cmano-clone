using System.Text;
using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class MessageLogAttributionAndDeepLinkTests
{
    [Test]
    public void MessageLogProjection_populates_ActorId_AgentId_SourceKind_across_entries()
    {
        var log = new DecisionLog();
        log.Append(new DecisionRecord(
            1.0,
            new AgentId("agent-alpha"),
            new TargetId("u1"),
            AutonomyLevel.Assisted,
            OrderKind.Engage,
            new[] { new ScoredIntent(OrderKind.Engage, 0.9, RiskLevel.High) },
            "target in envelope",
            3,
            10,
            0.25,
            SimTick: 1));
        log.AppendPlayerOrder(new PlayerOrderRecord(0, 2.0, 2, new TargetId("u1"), OrderKind.Move, "player-ui"));
        log.AppendPolicyDenial(new PolicyDenialRecord(
            0, 3.0, 3, new AgentId("agent-alpha"), new TargetId("u1"), 42, FireAbortReason.WeaponsTight, OrderKind.Engage));
        log.AppendContactOverride(new ContactOverrideRecord(
            0, 4.0, 4, "c1", ContactOverrideAction.Reclassify, "Hostile", "Neutral"));

        var lines = MessageLogProjection.Project(log);
        Assert.That(lines, Has.Count.EqualTo(4));

        Assert.That(lines[0].ActorId, Is.EqualTo("agent-alpha"));
        Assert.That(lines[0].AgentId, Is.EqualTo("agent-alpha"));
        Assert.That(lines[0].SourceKind, Is.EqualTo("Agent"));

        Assert.That(lines[1].ActorId, Is.EqualTo("player-ui"));
        Assert.That(lines[1].AgentId, Is.Null);
        Assert.That(lines[1].SourceKind, Is.EqualTo("Player"));

        Assert.That(lines[2].ActorId, Is.EqualTo("agent-alpha"));
        Assert.That(lines[2].AgentId, Is.EqualTo("agent-alpha"));
        Assert.That(lines[2].SourceKind, Is.EqualTo("Policy"));

        Assert.That(lines[3].ActorId, Is.EqualTo("operator"));
        Assert.That(lines[3].SourceKind, Is.EqualTo("Operator"));
        Assert.That(lines[3].UnitId, Is.Null);

        var bound = MessageLogPanelBinder.Bind(lines);
        Assert.That(bound.Rows[0].ActorId, Is.EqualTo("agent-alpha"));
        Assert.That(bound.Rows[0].AgentId, Is.EqualTo("agent-alpha"));
        Assert.That(bound.Rows[0].SourceKind, Is.EqualTo("Agent"));
        Assert.That(bound.Rows[0].AttributionLabel, Does.Contain("Agent"));
        Assert.That(bound.Rows[0].AttributionLabel, Does.Contain("agent-alpha"));
    }

    [Test]
    public void MessageLogTextExport_golden_remains_byte_identical_with_attribution_fields()
    {
        var log = new DecisionLog();
        log.AppendEngagement(new EngagementRecord(
            0, 1, 1, new TargetId("u1"), 7, true, null, new TargetId("hostile-1"), "Gun"));
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            0, 2, 2, new TargetId("u1"), new TargetId("hostile-1"), 7,
            EngagementOutcomeCodes.Hit, 0.9));
        log.AppendPolicyDenial(new PolicyDenialRecord(
            0, 3, 3, new AgentId("a1"), new TargetId("u1"), 1,
            FireAbortReason.WeaponsTight, OrderKind.Engage));

        var rendered = MessageLogTextExport.Render(MessageLogProjection.Project(log));
        var goldenPath = Path.Combine(
            FindRepoRoot(),
            "src",
            "ProjectAegis.Delegation.Tests",
            "Projection",
            "Fixtures",
            "message-log-text-export.golden.txt");
        var golden = File.ReadAllText(goldenPath, Encoding.UTF8);

        Assert.That(Encoding.UTF8.GetBytes(rendered), Is.EqualTo(Encoding.UTF8.GetBytes(golden)));
    }

    [Test]
    public void DecisionLogDeepLinkProjection_Inspect_resolves_AgentDecision_fields_read_only()
    {
        var log = new DecisionLog();
        log.Append(new DecisionRecord(
            12.0,
            new AgentId("agent-bravo"),
            new TargetId("u2"),
            AutonomyLevel.SemiAutonomous,
            OrderKind.Engage,
            new[]
            {
                new ScoredIntent(OrderKind.Engage, 0.85, RiskLevel.High),
                new ScoredIntent(OrderKind.Hold, 0.15, RiskLevel.Low),
            },
            "hostile radar lock",
            5.5,
            10.0,
            0.42,
            SimTick: 12));

        var seqId = log.ChronologicalEntries()[0].SequenceId;
        var fingerprintBefore = log.ComputeFingerprint();
        var countBefore = log.ChronologicalEntries().Count;

        var detail = DecisionLogDeepLinkProjection.Inspect(log, seqId);

        Assert.That(detail.Found, Is.True);
        Assert.That(detail.SequenceId, Is.EqualTo(seqId));
        Assert.That(detail.SimTick, Is.EqualTo(12UL));
        Assert.That(detail.EntryKind, Is.EqualTo(OrderLogEntryKind.AgentDecision));
        Assert.That(detail.Category, Is.EqualTo("AGENT_DECISION"));
        Assert.That(detail.SourceKind, Is.EqualTo("Agent"));
        Assert.That(detail.ActorId, Is.EqualTo("agent-bravo"));
        Assert.That(detail.AgentId, Is.EqualTo("agent-bravo"));
        Assert.That(detail.UnitId, Is.EqualTo("u2"));
        Assert.That(detail.Rationale, Is.EqualTo("hostile radar lock"));
        Assert.That(detail.AutonomyLevel, Is.EqualTo("SemiAutonomous"));
        Assert.That(detail.AttentionLoad, Is.EqualTo(5.5));
        Assert.That(detail.AttentionBudget, Is.EqualTo(10.0));
        Assert.That(detail.RngDraw, Is.EqualTo(0.42));
        Assert.That(detail.ScoredAlternatives, Has.Count.EqualTo(2));
        Assert.That(log.ComputeFingerprint(), Is.EqualTo(fingerprintBefore));
        Assert.That(log.ChronologicalEntries(), Has.Count.EqualTo(countBefore));
    }

    [TestCase(PlayerInfoModel.DelegationFog)]
    [TestCase(PlayerInfoModel.TieredByAutonomy)]
    public void DecisionLogDeepLinkProjection_InspectLive_hides_fogged_decisions_without_existence_oracle(PlayerInfoModel model)
    {
        var log = new DecisionLog();
        log.Append(new DecisionRecord(
            1.0,
            new AgentId("assisted-agent"),
            new TargetId("u1"),
            AutonomyLevel.Assisted,
            OrderKind.Hold,
            Array.Empty<ScoredIntent>(),
            "visible",
            1,
            10,
            0.1,
            SimTick: 1));
        log.Append(new DecisionRecord(
            2.0,
            new AgentId("full-agent"),
            new TargetId("u1"),
            AutonomyLevel.FullAutonomous,
            OrderKind.Engage,
            Array.Empty<ScoredIntent>(),
            "fogged",
            2,
            10,
            0.2,
            SimTick: 2));

        var visibleSeq = log.ChronologicalEntries()[0].SequenceId;
        var foggedSeq = log.ChronologicalEntries()[1].SequenceId;

        var visible = DecisionLogDeepLinkProjection.InspectLive(log, model, visibleSeq);
        var fogged = DecisionLogDeepLinkProjection.InspectLive(log, model, foggedSeq);
        var nonexistent = DecisionLogDeepLinkProjection.InspectLive(log, model, 999999UL);

        Assert.That(visible.Found, Is.True);
        Assert.That(visible.AgentId, Is.EqualTo("assisted-agent"));

        Assert.That(fogged.Found, Is.False);
        Assert.That(fogged.UnavailableReason, Is.EqualTo(DecisionLogDeepLinkProjection.UnavailableReasonCode));
        Assert.That(nonexistent.Found, Is.False);
        Assert.That(nonexistent.UnavailableReason, Is.EqualTo(DecisionLogDeepLinkProjection.UnavailableReasonCode));
        Assert.That(fogged.Summary, Is.EqualTo(nonexistent.Summary));
        Assert.That(fogged.ActorId, Is.Null);
        Assert.That(fogged.AgentId, Is.Null);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ProjectAegis.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repo root.");
    }
}
