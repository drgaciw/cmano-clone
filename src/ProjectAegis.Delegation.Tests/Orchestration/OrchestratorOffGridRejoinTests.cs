using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Hindsight;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Trust;
using ProjectAegis.Sim.Comms;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;

namespace ProjectAegis.Delegation.Tests.Orchestration;

[TestFixture]
public sealed class OrchestratorOffGridRejoinTests
{
    private static ObservedState State(double t) =>
        new(t, 0, 0, new Dictionary<TargetId, bool>());

    [Test]
    public void Tick_rejoin_with_informative_observation_updates_order_log_projections_and_aar()
    {
        var orchestrator = new DelegationOrchestrator(42)
        {
            CommsGrid = new CommsGridRegistry(new[]
            {
                new ScenarioCommsGridTransition(10, "u1", "OffGrid", "below comms depth"),
                new ScenarioCommsGridTransition(20, "u1", "OnGrid", "periscope depth"),
            }),
        };
        orchestrator.Register(new UnitTarget(new TargetId("u1")));
        orchestrator.BeginExecution();

        orchestrator.Tick(State(10));
        Assert.That(orchestrator.DecisionLog.ChronologicalEntries(), Has.Count.EqualTo(1)); // ModeChange Planning -> Executing

        orchestrator.CommsGridRejoinReconciler.RecordUnitObservation("u1", "hostile-1", ContactLifecycleState.Identified);
        orchestrator.Tick(State(20));

        Assert.That(orchestrator.DecisionLog.CommsGridRejoins, Has.Count.EqualTo(1));
        var rejoin = orchestrator.DecisionLog.CommsGridRejoins[0];
        Assert.That(rejoin.UnitId, Is.EqualTo("u1"));
        Assert.That(rejoin.OffGridSinceTick, Is.EqualTo(10UL));
        Assert.That(rejoin.RejoinTick, Is.EqualTo(20UL));
        Assert.That(rejoin.InformativeMerges, Has.Count.EqualTo(1));

        Assert.That(orchestrator.DecisionLog.ContactChanges, Has.Count.EqualTo(1));
        Assert.That(orchestrator.DecisionLog.ContactChanges[0].ContactId, Is.EqualTo("dl-hostile-1"));
        Assert.That(orchestrator.DecisionLog.ContactChanges[0].NewState, Is.EqualTo("Identified"));

        var messages = MessageLogProjection.Project(orchestrator.DecisionLog);
        var commsLine = messages.Single(m => m.Category == "COMMS");
        Assert.That(commsLine.Text, Does.Contain("u1").And.Contain("hostile-1"));

        var rejoinEntry = orchestrator.DecisionLog.ChronologicalEntries()
            .Single(e => e.Kind == OrderLogEntryKind.CommsGridRejoin);
        var deepLink = DecisionLogDeepLinkProjection.Inspect(orchestrator.DecisionLog, rejoinEntry.SequenceId);
        Assert.That(deepLink.Found, Is.True);
        Assert.That(deepLink.EntryKind, Is.EqualTo(OrderLogEntryKind.CommsGridRejoin));
        Assert.That(deepLink.Category, Is.EqualTo("COMMS"));
        Assert.That(deepLink.UnitId, Is.EqualTo("u1"));

        var sink = new CapturingMemoryClient();
        var finalizer = new HindsightSessionFinalizer(
            sink,
            new HindsightOptions { Enabled = true, ScenarioSlug = "baltic", RunId = "rejoin-test" });
        finalizer.OnScenarioFinalized(orchestrator.DecisionLog, Array.Empty<TrustSignal>(), true, 1.0);
        Assert.That(sink.Retained.First(r => r.BankId == "aar-baltic-rejoin-test").Content, Does.Contain("comms_rejoin_merges=1"));
    }

    [Test]
    public void Tick_rejoin_with_no_informative_merges_emits_zero_rejoin_entries()
    {
        var orchestrator = new DelegationOrchestrator(42)
        {
            CommsGrid = new CommsGridRegistry(new[]
            {
                new ScenarioCommsGridTransition(10, "u1", "OffGrid", "below comms depth"),
                new ScenarioCommsGridTransition(20, "u1", "OnGrid", "periscope depth"),
            }),
        };
        orchestrator.Register(new UnitTarget(new TargetId("u1")));
        orchestrator.BeginExecution();

        orchestrator.Tick(State(10));
        orchestrator.Tick(State(20));

        Assert.That(orchestrator.DecisionLog.CommsGridRejoins, Is.Empty);
        Assert.That(orchestrator.DecisionLog.ContactChanges, Is.Empty);
        Assert.That(orchestrator.DecisionLog.ChronologicalEntries().Select(e => e.Kind),
            Is.EqualTo(new[] { OrderLogEntryKind.ModeChange }));
    }

    [Test]
    public void Orchestrator_without_comms_grid_produces_identical_fingerprint_and_aar()
    {
        static (string Fingerprint, string Aar) RunOnce()
        {
            var orchestrator = new DelegationOrchestrator(7);
            orchestrator.Register(new UnitTarget(new TargetId("u1")));
            orchestrator.BeginExecution();
            orchestrator.Tick(State(1));
            orchestrator.Tick(State(2));

            var sink = new CapturingMemoryClient();
            var finalizer = new HindsightSessionFinalizer(
                sink,
                new HindsightOptions { Enabled = true, ScenarioSlug = "baltic", RunId = "legacy" });
            finalizer.OnScenarioFinalized(orchestrator.DecisionLog, Array.Empty<TrustSignal>(), true, 1.0);
            return (orchestrator.DecisionLog.ComputeFingerprint(), sink.Retained[0].Content);
        }

        var (fpA, aarA) = RunOnce();
        var (fpB, aarB) = RunOnce();

        Assert.That(fpA, Is.EqualTo(fpB));
        Assert.That(aarA, Is.EqualTo(aarB));
        Assert.That(aarA, Does.Not.Contain("comms_rejoin_merges"));
    }

    private sealed class CapturingMemoryClient : IHindsightMemoryClient
    {
        public List<(string BankId, string Content)> Retained { get; } = new();

        public void RetainFireAndForget(string bankId, string content, string? context = null) =>
            Retained.Add((bankId, content));

        public Task RetainAsync(string bankId, string content, CancellationToken cancellationToken = default)
        {
            Retained.Add((bankId, content));
            return Task.CompletedTask;
        }

        public Task<string?> ReflectAsync(string bankId, string query, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);
    }
}
