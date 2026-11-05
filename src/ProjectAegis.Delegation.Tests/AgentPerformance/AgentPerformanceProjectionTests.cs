using System.Collections;
using NUnit.Framework;
using ProjectAegis.Delegation.AgentPerformance;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Sim.Policy;

namespace ProjectAegis.Delegation.Tests.AgentPerformance;

[TestFixture]
public sealed class AgentPerformanceProjectionTests
{
    private readonly AgentPerformanceProjection _projection = new();

    [Test]
    public void Multiple_agents_count_chosen_intents_and_denials_without_counting_candidates()
    {
        var log = new DecisionLog();
        AppendDecision(log, "bravo", OrderKind.Engage, candidates: 4);
        AppendDecision(log, "alpha", OrderKind.Move, candidates: 2);
        AppendDecision(log, "alpha", OrderKind.Hold, candidates: 0);
        AppendDenial(log, "bravo");
        AppendDenial(log, "bravo");
        AppendDenial(log, "denial-only");
        AppendControlChange(log, "Agent", "Human", "alpha");

        var report = _projection.Project(log);

        Assert.That(report.Agents.Select(row => row.AgentId.Value),
            Is.EqualTo(new[] { "alpha", "bravo", "denial-only" }));
        Assert.That(report.Agents[0].IntentCount, Is.EqualTo(2));
        Assert.That(report.Agents[0].PolicyDenialCount, Is.Zero);
        Assert.That(report.Agents[0].OverrideCount, Is.EqualTo(1));
        Assert.That(report.Agents[0].OverrideRate, Is.EqualTo(0.5));
        Assert.That(report.Agents[1].IntentCount, Is.EqualTo(1));
        Assert.That(report.Agents[1].PolicyDenialCount, Is.EqualTo(2));
        Assert.That(report.Agents[1].OverrideRate, Is.EqualTo(0));
        Assert.That(report.Agents[2].IntentCount, Is.Zero);
        Assert.That(report.Agents[2].PolicyDenialCount, Is.EqualTo(1));
        Assert.That(report.Agents[2].OverrideRate, Is.Null);
    }

    [Test]
    public void Only_explicit_agent_to_human_changes_are_attributed_overrides()
    {
        var log = new DecisionLog();
        AppendDecision(log, "previous-owner");
        AppendControlChange(log, "Agent", "Human", "explicit-agent");
        AppendControlChange(log, "Human", "Agent", "released-agent");
        AppendControlChange(log, "Agent", "Agent", "reassigned-agent");
        AppendControlChange(log, "None", "Human", "not-an-agent");
        AppendControlChange(log, "AgentSuspended", "Human", "suspended-agent");
        AppendControlChange(log, "agent", "Human", "noncanonical-agent");
        AppendControlChange(log, "Agent", "human", "noncanonical-human");
        log.AppendPlayerOrder(new PlayerOrderRecord(0, 1, 1, new TargetId("unit"), OrderKind.Engage));

        var report = _projection.Project(log);

        Assert.That(report.Agents.Select(row => row.AgentId.Value),
            Is.EqualTo(new[] { "explicit-agent", "previous-owner" }));
        Assert.That(report.Agents[0].OverrideCount, Is.EqualTo(1));
        Assert.That(report.Agents[0].IntentCount, Is.Zero);
        Assert.That(report.Agents[0].OverrideRate, Is.Null);
        Assert.That(report.Agents[1].OverrideCount, Is.Zero);
        Assert.That(report.UnattributedOverrideCount, Is.Zero);
    }

    [Test]
    public void Missing_override_attribution_is_reported_without_inference_from_unit_history()
    {
        var log = new DecisionLog();
        AppendDecision(log, "previous-owner");
        AppendControlChange(log, "Agent", "Human", null);
        AppendControlChange(log, "Agent", "Human", "");
        AppendControlChange(log, "Agent", "Human", " ");
        AppendControlChange(log, "Human", "Agent", null);
        AppendControlChange(log, "None", "Human", null);

        var report = _projection.Project(log);

        Assert.That(report.UnattributedOverrideCount, Is.EqualTo(3));
        Assert.That(report.Agents, Has.Count.EqualTo(1));
        Assert.That(report.Agents[0].AgentId.Value, Is.EqualTo("previous-owner"));
        Assert.That(report.Agents[0].OverrideCount, Is.Zero);
        Assert.That(report.Agents[0].OverrideRate, Is.Zero);
    }

    [Test]
    public void Override_rate_is_interventions_per_intent_and_can_exceed_one()
    {
        var log = new DecisionLog();
        AppendDecision(log, "alpha");
        AppendControlChange(log, "Agent", "Human", "alpha");
        AppendControlChange(log, "Human", "Agent", "alpha");
        AppendControlChange(log, "Agent", "Human", "alpha");

        var row = _projection.Project(log).Agents.Single();

        Assert.That(row.OverrideCount, Is.EqualTo(2));
        Assert.That(row.IntentCount, Is.EqualTo(1));
        Assert.That(row.OverrideRate, Is.EqualTo(2.0));
    }

    [Test]
    public void No_decisions_preserves_explicit_overrides_and_undefined_rate()
    {
        var log = new DecisionLog();
        AppendControlChange(log, "Agent", "Human", "alpha");
        AppendControlChange(log, "Agent", "Human", null);

        var report = _projection.Project(log);

        Assert.That(report.Agents, Has.Count.EqualTo(1));
        Assert.That(report.Agents[0].IntentCount, Is.Zero);
        Assert.That(report.Agents[0].OverrideRate, Is.Null);
        Assert.That(report.UnattributedOverrideCount, Is.EqualTo(1));
    }

    [Test]
    public void Empty_log_returns_empty_read_only_report()
    {
        var report = _projection.Project(new DecisionLog());

        Assert.That(report.Agents, Is.Empty);
        Assert.That(report.UnattributedOverrideCount, Is.Zero);
    }

    [Test]
    public void Agent_order_is_ordinal_and_independent_of_event_order()
    {
        var first = new DecisionLog();
        var second = new DecisionLog();
        var ids = new[] { "zeta", "alpha", "Zulu", "Alpha", "ä" };
        foreach (var id in ids)
        {
            AppendDecision(first, id);
        }

        foreach (var id in ids.Reverse())
        {
            AppendDecision(second, id);
        }

        Assert.That(_projection.Project(first).Agents.Select(row => row.AgentId.Value),
            Is.EqualTo(new[] { "Alpha", "Zulu", "alpha", "zeta", "ä" }));
        Assert.That(_projection.Project(first).Agents, Is.EqualTo(_projection.Project(second).Agents));
    }

    [Test]
    public void Repeated_projection_preserves_source_entries_and_replay_fingerprint()
    {
        var log = new DecisionLog();
        AppendDecision(log, "alpha");
        AppendDenial(log, "alpha");
        AppendControlChange(log, "Agent", "Human", "alpha");
        var before = log.ChronologicalEntries().ToArray();
        var fingerprint = log.ComputeFingerprint();

        var first = _projection.Project(log);
        var second = _projection.Project(log);

        Assert.That(first.Agents, Is.EqualTo(second.Agents));
        Assert.That(first.UnattributedOverrideCount, Is.EqualTo(second.UnattributedOverrideCount));
        Assert.That(log.ChronologicalEntries(), Is.EqualTo(before));
        Assert.That(log.ComputeFingerprint(), Is.EqualTo(fingerprint));
    }

    [Test]
    public void Report_is_detached_and_cannot_be_changed_through_collection_casts()
    {
        var log = new DecisionLog();
        AppendDecision(log, "alpha");
        var report = _projection.Project(log);
        AppendDecision(log, "alpha");
        AppendDecision(log, "bravo");

        Assert.That(report.Agents, Has.Count.EqualTo(1));
        Assert.That(report.Agents[0].IntentCount, Is.EqualTo(1));
        Assert.That(((IList)report.Agents).IsReadOnly, Is.True);
        Assert.Throws<NotSupportedException>(() => ((IList)report.Agents).Clear());
    }

    [Test]
    public void Read_only_interface_is_sufficient_and_typed_views_are_not_double_counted()
    {
        var source = new DecisionLog();
        AppendDecision(source, "alpha");
        AppendDenial(source, "alpha");
        AppendControlChange(source, "Agent", "Human", "alpha");
        IReadOnlyOrderLog log = new ChronologicalReadOnlyLog(source.ChronologicalEntries());

        var report = _projection.Project(log);

        Assert.That(report.Agents.Single().IntentCount, Is.EqualTo(1));
        Assert.That(report.Agents.Single().PolicyDenialCount, Is.EqualTo(1));
        Assert.That(report.Agents.Single().OverrideCount, Is.EqualTo(1));
    }

    [Test]
    public void Null_log_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => _projection.Project(null!));
    }

    private static void AppendDecision(DecisionLog log, string agentId, OrderKind kind = OrderKind.Move, int candidates = 1)
    {
        var intents = Enumerable.Range(0, candidates)
            .Select(_ => new ScoredIntent(kind, 1, RiskLevel.Low)).ToArray();
        var payload = new AgentDecisionPayload(1, 1, new AgentId(agentId), new TargetId("unit"),
            AutonomyLevel.FullAutonomous, kind, intents, "chosen", 1, 10, 0.5);
        log.Append(new OrderLogEntry(0, OrderLogEntryKind.AgentDecision, 1, payload));
    }

    private static void AppendDenial(DecisionLog log, string agentId) =>
        log.AppendPolicyDenial(new PolicyDenialRecord(0, 1, 1, new AgentId(agentId),
            new TargetId("unit"), 1, FireAbortReason.RoeHoldFire, OrderKind.Engage));

    private static void AppendControlChange(DecisionLog log, string previous, string next, string? agentId) =>
        log.AppendControllerChange(new ControllerChangeRecord(0, 1, new TargetId("unit"), previous, next,
            agentId is null ? null : new AgentId(agentId)));

    private sealed class ChronologicalReadOnlyLog(IReadOnlyList<OrderLogEntry> entries) : IReadOnlyOrderLog
    {
        public IReadOnlyList<OrderLogEntry> ChronologicalEntries() => entries;
        public string ComputeFingerprint() => throw new NotSupportedException();
        public IReadOnlyList<DecisionRecord> Records => throw new NotSupportedException();
        public IReadOnlyList<PolicyDenialRecord> PolicyDenials => throw new NotSupportedException();
        public IReadOnlyList<EngagementRecord> Engagements => throw new NotSupportedException();
        public IReadOnlyList<ControllerChangeRecord> ControllerChanges => throw new NotSupportedException();
    }
}

