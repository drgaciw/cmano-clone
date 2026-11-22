namespace ProjectAegis.Delegation.UnityAdapter.CommandReview;

using Bridge;
using C2Nodes;
using Core;
using MissionIntent;

/// <summary>
/// Live authored coordination facts and task-group metadata for Play Mode smoke acceptance (DRG-239).
/// Supplies one named task group (<see cref="TaskGroupId"/>), explicit mission-package role assignment,
/// advisory mission intent, and source-labelled finite normalized <c>[0..1]</c> coverage polygons.
///
/// Lives in <c>CommandReview</c> so namespace dependency flow remains acyclic (<c>CommandReview → Bridge</c>).
/// Presentation boundary: ADR-010 §2–3, ADR-007, ADR-001 (read-only projection inputs; group orders
/// flow strictly through <see cref="CoordinationCommandBridge"/> / <see cref="C2PlayerCommandBridge"/>).
///
/// Enables owner Play Mode signoff for DRG-175, DRG-185, DRG-192, DRG-235, and DRG-244;
/// does NOT replace DRG-208 owner acceptance.
/// </summary>
public sealed class PlayModeSmokeCoordinationFactsProvider : ICoordinationFacts
{
    /// <summary>Canonical smoke task-group identifier registered by <see cref="PlayModeSmokeOrbatSeeder"/>.</summary>
    public const string TaskGroupId = PlayModeSmokeOrbatSeeder.TaskGroupId;

    /// <summary>Entity key assigned to <see cref="TaskGroupId"/> in the smoke registry.</summary>
    public static readonly EntityKey TaskGroupEntityKey = PlayModeSmokeOrbatSeeder.TaskGroupEntityKey;

    /// <summary>Primary friendly unit acting as task-group C2 lead.</summary>
    public const string FriendlyLeadUnitId = PlayModeSmokeOrbatSeeder.FriendlyUnitId;

    /// <summary>Second friendly escort wingman unit for multi-member coordination and gap tests.</summary>
    public const string FriendlyEscortUnitId = PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId;

    /// <summary>Entity key assigned to <see cref="FriendlyEscortUnitId"/> when seeded.</summary>
    public static readonly EntityKey FriendlyEscortEntityKey = PlayModeSmokeOrbatSeeder.FriendlyEscortEntityKey;

    /// <summary>Authored mission-package identifier assigned to <see cref="TaskGroupId"/>.</summary>
    public const string PackageId = "pkg-baltic-escort";

    /// <summary>Display label for the authored smoke mission package.</summary>
    public const string PackageLabel = "Baltic Escort Patrol Package";

    /// <summary>Package element identifier for the C2 lead unit.</summary>
    public const string C2ElementId = "smoke-c2-lead";

    /// <summary>Package element identifier for the escort shooter unit.</summary>
    public const string EscortElementId = "smoke-escort-shooter";

    /// <summary>Coverage identifier owned by <see cref="C2ElementId"/>.</summary>
    public const string C2CoverageId = "cov-baltic-c2-sector";

    /// <summary>Human-readable label for the C2 lead coverage polygon.</summary>
    public const string C2CoverageLabel = "Baltic Escort C2 Sector";

    /// <summary>Explicit provenance string for the C2 lead coverage polygon.</summary>
    public const string C2CoverageSourceRef =
        "fixture:PlayModeSmokeCoordinationFactsProvider/baltic-escort-c2-sector";

    /// <summary>Coverage identifier owned by <see cref="EscortElementId"/>.</summary>
    public const string EscortCoverageId = "cov-baltic-escort-screen";

    /// <summary>Human-readable label for the escort area-defense coverage polygon.</summary>
    public const string EscortCoverageLabel = "Baltic Escort Area Screen";

    /// <summary>Explicit provenance string for the escort coverage polygon.</summary>
    public const string EscortCoverageSourceRef =
        "fixture:PlayModeSmokeCoordinationFactsProvider/baltic-escort-screen";

    /// <summary>Initializes a provider with explicit authored coordination facts.</summary>
    public PlayModeSmokeCoordinationFactsProvider(
        IReadOnlyList<PackageDefinition> packages,
        IReadOnlyList<CoverageFact> coverage,
        IReadOnlyList<MissionIntentInput> intents,
        IReadOnlyList<CoordinationGroupPackageAssignment> assignments)
    {
        Packages = packages ?? throw new ArgumentNullException(nameof(packages));
        Coverage = coverage ?? throw new ArgumentNullException(nameof(coverage));
        Intents = intents ?? throw new ArgumentNullException(nameof(intents));
        Assignments = assignments ?? throw new ArgumentNullException(nameof(assignments));
    }

    /// <inheritdoc />
    public IReadOnlyList<PackageDefinition> Packages { get; }

    /// <inheritdoc />
    public IReadOnlyList<CoverageFact> Coverage { get; }

    /// <inheritdoc />
    public IReadOnlyList<MissionIntentInput> Intents { get; }

    /// <inheritdoc />
    public IReadOnlyList<CoordinationGroupPackageAssignment> Assignments { get; }

    /// <summary>
    /// Creates the default authored smoke coordination facts: one package (<see cref="PackageId"/>),
    /// explicit assignment to <see cref="TaskGroupId"/>, Hold/NoStrike advisory intent with Withdraw
    /// retask advice, and two source-labelled finite normalized <c>[0..1]</c> coverage polygons.
    /// </summary>
    public static PlayModeSmokeCoordinationFactsProvider CreateDefault() =>
        new(
            packages: CreateDefaultPackages(),
            coverage: CreateDefaultCoverage(),
            intents: CreateDefaultIntents(),
            assignments: [new CoordinationGroupPackageAssignment(TaskGroupId, PackageId)]);

    /// <summary>
    /// Negative-case factory: supplies the same packages, coverage, and intent WITHOUT an explicit
    /// group-to-package assignment so overlap alone stays explicitly <c>UNKNOWN</c>.
    /// </summary>
    public static PlayModeSmokeCoordinationFactsProvider CreateWithoutAssignment() =>
        new(
            packages: CreateDefaultPackages(),
            coverage: CreateDefaultCoverage(),
            intents: CreateDefaultIntents(),
            assignments: Array.Empty<CoordinationGroupPackageAssignment>());

    /// <summary>
    /// Wraps an existing <see cref="ISimWorldSnapshot"/> with authored <see cref="ICoordinationFacts"/>
    /// so <see cref="CoordinationBridge.Build(DelegationBridge?, ISimWorldSnapshot?)"/> and Unity hosts
    /// consume live task-group/coverage facts without touching <c>DelegationBridge.cs</c>.
    /// </summary>
    public static ISimWorldSnapshot WrapSnapshot(
        ISimWorldSnapshot inner,
        ICoordinationFacts? facts = null)
    {
        if (inner == null)
        {
            throw new ArgumentNullException(nameof(inner));
        }

        return new PlayModeSmokeCoordinationSnapshot(inner, facts ?? CreateDefault());
    }

    /// <summary>
    /// Projects a <see cref="CoordinationSnapshot"/> using the live smoke facts provider.
    /// </summary>
    public static CoordinationSnapshot BuildSnapshot(
        DelegationBridge? bridge,
        ISimWorldSnapshot? snapshot,
        ICoordinationFacts? facts = null) =>
        CoordinationBridge.Build(bridge, snapshot, facts ?? CreateDefault());

    private static IReadOnlyList<PackageDefinition> CreateDefaultPackages() =>
    [
        new PackageDefinition(
            PackageId,
            PackageLabel,
            [
                new PackageElementDefinition(
                    C2ElementId,
                    FriendlyLeadUnitId,
                    C2NodeRole.C2,
                    "organic-c2-air-surface"),
                new PackageElementDefinition(
                    EscortElementId,
                    FriendlyEscortUnitId,
                    C2NodeRole.Shooter,
                    "escort-area-defense"),
            ]),
    ];

    private static IReadOnlyList<CoverageFact> CreateDefaultCoverage() =>
    [
        new CoverageFact(
            C2ElementId,
            C2CoverageId,
            C2CoverageLabel,
            new CoverageAreaGeometry(
                [
                    new CoverageAreaPoint(0.12, 0.18),
                    new CoverageAreaPoint(0.54, 0.18),
                    new CoverageAreaPoint(0.58, 0.56),
                    new CoverageAreaPoint(0.16, 0.52),
                ],
                C2CoverageSourceRef)),
        new CoverageFact(
            EscortElementId,
            EscortCoverageId,
            EscortCoverageLabel,
            new CoverageAreaGeometry(
                [
                    new CoverageAreaPoint(0.46, 0.28),
                    new CoverageAreaPoint(0.86, 0.24),
                    new CoverageAreaPoint(0.82, 0.76),
                    new CoverageAreaPoint(0.42, 0.72),
                ],
                EscortCoverageSourceRef)),
    ];

    private static IReadOnlyList<MissionIntentInput> CreateDefaultIntents() =>
    [
        new MissionIntentInput(
            TaskGroupId,
            string.Empty,
            MissionIntentCode.Hold,
            [MissionIntentConstraintCode.NoStrike],
            MissionIntentRetaskAdvice.Withdraw),
    ];

    private sealed class PlayModeSmokeCoordinationSnapshot : ISimWorldSnapshot, ICoordinationFacts
    {
        private readonly ISimWorldSnapshot _inner;
        private readonly ICoordinationFacts _facts;

        public PlayModeSmokeCoordinationSnapshot(ISimWorldSnapshot inner, ICoordinationFacts facts)
        {
            _inner = inner;
            _facts = facts;
        }

        public double SimTime => _inner.SimTime;
        public int ContactCount => _inner.ContactCount;
        public int ActiveEngagementCount => _inner.ActiveEngagementCount;
        public TargetId? PrimaryHostileContactId => _inner.PrimaryHostileContactId;
        public bool HasFireControlTrackOnPrimaryContact => _inner.HasFireControlTrackOnPrimaryContact;
        public bool ObserverRadarEmconActive => _inner.ObserverRadarEmconActive;
        public bool PrimaryHostileDestroyed => _inner.PrimaryHostileDestroyed;
        public TargetId? PrimaryBlueForceContactId => _inner.PrimaryBlueForceContactId;
        public bool PrimaryBlueForceContactDestroyed => _inner.PrimaryBlueForceContactDestroyed;
        public IReadOnlyDictionary<string, string>? PreferredHostileByShooter => _inner.PreferredHostileByShooter;
        public bool IsMemberAlive(TargetId memberId) => _inner.IsMemberAlive(memberId);
        public bool TryGetKinematicPose(string unitOrContactId, out UnitKinematicPose pose) =>
            _inner.TryGetKinematicPose(unitOrContactId, out pose);
        public IReadOnlyList<CourseWaypoint>? GetPlottedCourse(string unitId) =>
            _inner.GetPlottedCourse(unitId);

        public IReadOnlyList<PackageDefinition> Packages => _facts.Packages;
        public IReadOnlyList<CoverageFact> Coverage => _facts.Coverage;
        public IReadOnlyList<MissionIntentInput> Intents => _facts.Intents;
        public IReadOnlyList<CoordinationGroupPackageAssignment> Assignments => _facts.Assignments;
    }
}
