namespace ProjectAegis.Delegation.UnityAdapter.Baltic;

using System.Text.Json;
using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;

/// <summary>DRG-166 synthetic, headless six-leg acceptance scenario for Combat UX Slice B.</summary>
/// <remarks>This is test evidence, not a production catalog or OSINT claim.</remarks>
public static class SliceBCombatScenario
{
    public sealed record Definition(string Description, bool Synthetic, IReadOnlyList<Leg> Legs);

    public sealed record Leg(
        string ShooterId,
        string TargetId,
        string WeaponFamilyId,
        bool Permitted,
        double DistanceMeters,
        double MinRangeMeters,
        double MaxRangeMeters,
        int RoundsRemaining,
        int SalvoSize,
        bool HasFireControlTrack,
        float ShooterX,
        float ShooterY,
        float TargetX,
        float TargetY);

    public sealed record Result(
        DecisionLog Log,
        CombatEventSnapshot Events,
        string Fingerprint,
        IReadOnlyList<MapSymbolEntry> Symbols);

    /// <summary>Loads the repository fixture and runs all legs with the supplied deterministic seed.</summary>
    public static Result Run(int seed = 7) => Run(seed, LoadDefaultDefinition());

    /// <summary>Runs every fixture leg through a real <see cref="SimulationSession"/> and resolver.</summary>
    public static Result Run(int seed, Definition definition)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }
        if (!definition.Synthetic || definition.Legs.Count == 0)
        {
            throw new ArgumentException("Slice B acceptance input must be a non-empty synthetic fixture.", nameof(definition));
        }

        var aggregate = new DecisionLog();
        var symbols = new List<MapSymbolEntry>(definition.Legs.Count * 2);
        for (var i = 0; i < definition.Legs.Count; i++)
        {
            var leg = definition.Legs[i];
            Validate(leg);
            var context = new EngageContext(
                leg.DistanceMeters,
                new WeaponEnvelope(leg.MinRangeMeters, leg.MaxRangeMeters),
                leg.RoundsRemaining,
                leg.HasFireControlTrack,
                SalvoSize: leg.SalvoSize);
            var session = SimulationSession.BindMvpEngagement(
                new DelegationOrchestrator(seed + i),
                context,
                defaultMagazineRounds: leg.RoundsRemaining,
                weaponFamilyId: leg.WeaponFamilyId);
            WireEngageAgent(session, leg.ShooterId);
            session.BeginExecution();
            session.Tick(new ObservedState(
                SimTime: i + 1,
                ContactCount: 1,
                ActiveEngagementCount: 0,
                MemberAlive: new Dictionary<TargetId, bool>(),
                HasFireControlTrack: leg.HasFireControlTrack,
                PrimaryHostileContactId: new TargetId(leg.TargetId)));

            CopyCombatRows(session.Orchestrator.DecisionLog, aggregate);
            symbols.Add(new MapSymbolEntry(
                leg.ShooterId, "Friendly", "■", leg.ShooterId,
                leg.ShooterX, leg.ShooterY, IsDestroyed: false));
            symbols.Add(new MapSymbolEntry(
                leg.TargetId, "Hostile", "◆", leg.TargetId,
                leg.TargetX, leg.TargetY,
                IsDestroyed: session.Orchestrator.DecisionLog.EngagementOutcomes.Any(o =>
                    o.VictimTargetId.Value == leg.TargetId && o.OutcomeCode == EngagementOutcomeCodes.Kill)));
        }

        var events = CombatEventLogProjection.Build(aggregate, double.MaxValue);
        return new Result(
            aggregate,
            events,
            CombatEventFingerprint.Compute(events),
            Array.AsReadOnly(symbols.ToArray()));
    }

    private static void CopyCombatRows(DecisionLog source, DecisionLog destination)
    {
        foreach (var engagement in source.Engagements.OrderBy(e => e.SequenceId))
        {
            destination.AppendEngagement(engagement with { SequenceId = 0 });
        }

        foreach (var outcome in source.EngagementOutcomes.OrderBy(o => o.SequenceId))
        {
            destination.AppendEngagementOutcome(outcome with { SequenceId = 0 });
        }
    }

    private static void WireEngageAgent(SimulationSession session, string shooterId)
    {
        var unit = new UnitTarget(new TargetId(shooterId));
        var agent = session.Orchestrator.CreateAgent(
            new AgentId($"agent-{shooterId}"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            policy: new EngageOnlyPolicy());
        session.Orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);
        session.Orchestrator.Register(unit);
    }

    private static void Validate(Leg leg)
    {
        if (string.IsNullOrWhiteSpace(leg.ShooterId)
            || string.IsNullOrWhiteSpace(leg.TargetId)
            || string.IsNullOrWhiteSpace(leg.WeaponFamilyId)
            || leg.RoundsRemaining < 0
            || leg.SalvoSize < 1
            || leg.MinRangeMeters < 0
            || leg.MaxRangeMeters < leg.MinRangeMeters)
        {
            throw new ArgumentException("Slice B acceptance leg is invalid.", nameof(leg));
        }
    }

    private static Definition LoadDefaultDefinition()
    {
        var directory = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            var path = Path.Combine(directory, "data", "scenarios", "slice-b-combat-acceptance.json");
            if (File.Exists(path))
            {
                return JsonSerializer.Deserialize<Definition>(
                    File.ReadAllText(path),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new InvalidDataException($"Invalid Slice B acceptance fixture: {path}");
            }

            directory = Path.GetDirectoryName(directory) ?? directory;
        }

        throw new FileNotFoundException("slice-b-combat-acceptance.json");
    }

    private sealed class EngageOnlyPolicy : IPolicy
    {
        public IReadOnlyList<ScoredIntent> GenerateCandidates(PerceivedState perceived, TraitVector traits)
        {
            _ = perceived;
            _ = traits;
            return [new ScoredIntent(OrderKind.Engage, 1, RiskLevel.High)];
        }
    }
}
