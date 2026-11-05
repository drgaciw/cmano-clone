namespace ProjectAegis.Delegation.UnityAdapter.CommandReview;

using ProjectAegis.Delegation.C2Nodes;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.MissionIntent;
using ProjectAegis.Delegation.UnityAdapter.Bridge;

/// <summary>One deterministic scenario unit and its adapter entity key.</summary>
public sealed record CoordinationScenarioUnit(EntityKey Entity, string UnitId);

/// <summary>All data inputs for the public Slice C coordination acceptance harness.</summary>
public sealed record CoordinationScenarioInput(
    int GlobalSeed,
    double SimTime,
    EntityKey GroupEntity,
    string GroupId,
    IReadOnlyList<CoordinationScenarioUnit> Units,
    PackageDefinition Package,
    CoverageFact Coverage,
    MissionIntentInput Intent,
    CoordinationDecision Decision);

/// <summary>One concrete unit effect emitted by the acceptance harness.</summary>
public sealed record CoordinationAppliedOrder(EntityKey Entity, string UnitId, OrderKind Kind);

/// <summary>Review, decision, log, and actual unit effects returned by the acceptance harness.</summary>
public sealed record CoordinationScenarioResult(
    DelegationBridge Bridge,
    ISimWorldSnapshot Snapshot,
    DecisionLog Log,
    CoordinationSnapshot Review,
    CoordinationDecisionResult Decision,
    IReadOnlyList<CoordinationAppliedOrder> AppliedOrders,
    CoordinationSnapshot AfterTick);

/// <summary>
/// Public deterministic Slice C acceptance harness. It exercises review, advisory intent,
/// explicit human decision, group command enqueue, simulation tick, and concrete unit effects.
/// </summary>
public static class CoordinationScenario
{
    /// <summary>
    /// Fixture-only defaults chosen to cover a two-unit C2/shooter package and one authoritative
    /// normalized sector. Product values should be supplied through <see cref="Run"/>.
    /// </summary>
    public static CoordinationScenarioInput CreateDefaultInput() => new(
        GlobalSeed: 175192,
        SimTime: 4,
        GroupEntity: new EntityKey(100),
        GroupId: "slice-c-group",
        Units:
        [
            new CoordinationScenarioUnit(new EntityKey(1), "slice-c-c2"),
            new CoordinationScenarioUnit(new EntityKey(2), "slice-c-shooter"),
        ],
        Package: new PackageDefinition(
            "slice-c-package",
            "Slice C acceptance package",
            [
                new PackageElementDefinition("c2-element", "slice-c-c2", C2NodeRole.C2, "organic-c2"),
                new PackageElementDefinition("shooter-element", "slice-c-shooter", C2NodeRole.Shooter, "package-engage"),
            ]),
        Coverage: new CoverageFact(
            "c2-element",
            "coordination-sector",
            "Acceptance coordination sector",
            new CoverageAreaGeometry(
                [new(0.15, 0.15), new(0.85, 0.15), new(0.50, 0.75)],
                "fixture:CoordinationScenario/default")),
        Intent: new MissionIntentInput(
            "slice-c-group",
            string.Empty,
            MissionIntentCode.Hold,
            [MissionIntentConstraintCode.NoStrike],
            MissionIntentRetaskAdvice.Withdraw),
        Decision: CoordinationDecision.Withdraw);

    /// <summary>Run the acceptance flow using only the supplied deterministic data.</summary>
    public static CoordinationScenarioResult Run(CoordinationScenarioInput input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }
        if (input.Units is null || input.Units.Count == 0)
        {
            throw new ArgumentException("At least one scenario unit is required.", nameof(input));
        }

        var bridge = new DelegationBridge(input.GlobalSeed, mvpEngagement: false);
        var group = bridge.Registry.RegisterGroup(input.GroupEntity, input.GroupId);
        group.Target.Slot.SetActive(new HumanController());
        foreach (var unit in input.Units)
        {
            var binding = bridge.Registry.RegisterUnit(unit.Entity, unit.UnitId);
            bridge.Registry.LinkGroupMember(group.TargetId, binding.TargetId);
        }

        var snapshot = new ScenarioSnapshot(input.SimTime, input.Units.Select(u => u.UnitId));
        var facts = new ScenarioFacts([input.Package], [input.Coverage], [input.Intent]);
        var review = CoordinationBridge.Build(bridge, snapshot, facts);
        var decision = CoordinationCommandBridge.Submit(
            bridge,
            snapshot,
            input.GroupId,
            input.Decision,
            review.Groups.FirstOrDefault(g =>
                string.Equals(g.Coordination.GroupId, input.GroupId, StringComparison.Ordinal))?.Intent);
        var recorder = new ScenarioOrderSink();
        bridge.BeginExecution();
        bridge.Tick(
            snapshot,
            new CoordinationOrderSink(
                bridge.Registry,
                snapshot,
                recorder,
                decision.ApprovedScope is null ? [] : [decision.ApprovedScope]));
        var afterTick = CoordinationBridge.Build(bridge, snapshot, facts);

        return new CoordinationScenarioResult(
            bridge,
            snapshot,
            bridge.Orchestrator.DecisionLog,
            review,
            decision,
            recorder.Applied,
            afterTick);
    }

    private sealed class ScenarioFacts(
        IReadOnlyList<PackageDefinition> packages,
        IReadOnlyList<CoverageFact> coverage,
        IReadOnlyList<MissionIntentInput> intents) : ICoordinationFacts
    {
        public IReadOnlyList<PackageDefinition> Packages { get; } = packages;
        public IReadOnlyList<CoverageFact> Coverage { get; } = coverage;
        public IReadOnlyList<MissionIntentInput> Intents { get; } = intents;
        public IReadOnlyList<CoordinationGroupPackageAssignment> Assignments { get; } =
            [new CoordinationGroupPackageAssignment(intents[0].GroupId, packages[0].PackageId)];
    }

    private sealed class ScenarioSnapshot(double simTime, IEnumerable<string> aliveIds) : ISimWorldSnapshot
    {
        private readonly HashSet<string> _aliveIds = new(aliveIds, StringComparer.Ordinal);
        public double SimTime { get; } = simTime;
        public int ContactCount => 0;
        public int ActiveEngagementCount => 0;
        public TargetId? PrimaryHostileContactId => null;
        public bool HasFireControlTrackOnPrimaryContact => false;
        public bool ObserverRadarEmconActive => true;
        public bool IsMemberAlive(TargetId memberId) => _aliveIds.Contains(memberId.Value);
    }

    private sealed class ScenarioOrderSink : IOrderSink
    {
        private readonly List<CoordinationAppliedOrder> _applied = [];
        public IReadOnlyList<CoordinationAppliedOrder> Applied => _applied;
        public void ApplyOrder(EntityKey entity, in Order order) =>
            _applied.Add(new CoordinationAppliedOrder(entity, order.Target.Value, order.Kind));
    }
}
