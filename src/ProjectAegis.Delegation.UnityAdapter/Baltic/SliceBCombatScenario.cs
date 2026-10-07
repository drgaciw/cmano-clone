namespace ProjectAegis.Delegation.UnityAdapter.Baltic;

using System.Text.Json;
using CombatEvents;
using Core;
using Decision;
using Orchestration;
using Policy;
using Projection;
using Sim;
using Targets;
using Traits;
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

    /// <summary>One approved player engage command: queued by a Manual agent, approved, then executed.</summary>
    public sealed record Command(string ShooterId, long OrderId, double CommandSimTime, double ExecutedSimTime);

    public sealed record Result(
        DecisionLog Log,
        CombatEventSnapshot Events,
        string Fingerprint,
        IReadOnlyList<MapSymbolEntry> Symbols)
    {
        /// <summary>DRG-166: approved player commands in leg order; empty for autonomous runs.</summary>
        public IReadOnlyList<Command> Commands { get; init; } = Array.Empty<Command>();
    }

    /// <summary>Synthetic sensor contact id for a fixture target (commanded runs only).</summary>
    public static string ContactIdFor(string targetId) => "contact-" + targetId;

    /// <summary>Loads the repository fixture and runs all legs with the supplied deterministic seed.</summary>
    public static Result Run(int seed = 7) => Run(seed, LoadDefaultDefinition());

    /// <summary>Runs every fixture leg through a real <see cref="SimulationSession"/> and resolver.</summary>
    public static Result Run(int seed, Definition definition) => Execute(seed, definition, commanded: false);

    /// <summary>
    /// DRG-166: loads the repository fixture and drives every leg from an approved player command.
    /// </summary>
    public static Result RunCommanded(int seed = 7) => RunCommanded(seed, LoadDefaultDefinition());

    /// <summary>
    /// DRG-166: each leg's shooter is a Manual agent. Its engage order is queued for approval, approved through
    /// <see cref="DelegationOrchestrator.TryApprovePendingOrder"/> (the player command), and executed by the real
    /// resolver on the next tick. A synthetic Detected → Classified → Identified contact feed precedes the command.
    /// </summary>
    public static Result RunCommanded(int seed, Definition definition) => Execute(seed, definition, commanded: true);

    private static Result Execute(int seed, Definition definition, bool commanded)
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
        var commands = new List<Command>(commanded ? definition.Legs.Count : 0);
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
            WireEngageAgent(session, leg.ShooterId, commanded ? AutonomyLevel.Manual : AutonomyLevel.FullAutonomous);
            session.BeginExecution();
            var observed = new ObservedState(
                SimTime: i + 1,
                ContactCount: 1,
                ActiveEngagementCount: 0,
                MemberAlive: new Dictionary<TargetId, bool>(),
                HasFireControlTrack: leg.HasFireControlTrack,
                PrimaryHostileContactId: new TargetId(leg.TargetId));
            if (commanded)
            {
                AppendContactDetection(aggregate, leg, i);
                var command = ApproveEngageCommand(session, leg.ShooterId, observed with { SimTime = i + 0.5 }, i + 1);
                session.Tick(observed);
                if (!session.Orchestrator.ExecutedOrders.Any(o => o.Id.Value == command.OrderId))
                {
                    throw new InvalidOperationException(
                        $"Approved engage command {command.OrderId} for '{leg.ShooterId}' did not execute.");
                }

                commands.Add(command);
            }
            else
            {
                session.Tick(observed);
            }

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
            Array.AsReadOnly(symbols.ToArray()))
        {
            Commands = Array.AsReadOnly(commands.ToArray()),
        };
    }

    private static Command ApproveEngageCommand(
        SimulationSession session,
        string shooterId,
        ObservedState commandState,
        double executeSimTime)
    {
        session.Tick(commandState);
        var pending = session.Orchestrator.PendingApprovals.FirstOrDefault(p =>
            p.Order.Kind == OrderKind.Engage
            && string.Equals(p.Order.Target.Value, shooterId, StringComparison.Ordinal));
        if (pending is null || !session.Orchestrator.TryApprovePendingOrder(pending.Order.Id))
        {
            throw new InvalidOperationException($"No pending engage order for '{shooterId}' to approve.");
        }

        return new Command(shooterId, pending.Order.Id.Value, commandState.SimTime, executeSimTime);
    }

    private static void AppendContactDetection(DecisionLog log, Leg leg, int index)
    {
        var contactId = ContactIdFor(leg.TargetId);
        var tick = (ulong)index;
        log.AppendContactChange(new ContactChangeRecord(
            0, index + 0.1, tick, leg.ShooterId, contactId, leg.TargetId, "Unknown", "Detected"));
        log.AppendContactChange(new ContactChangeRecord(
            0, index + 0.2, tick, leg.ShooterId, contactId, leg.TargetId, "Detected", "Classified"));
        log.AppendContactChange(new ContactChangeRecord(
            0, index + 0.3, tick, leg.ShooterId, contactId, leg.TargetId, "Classified", "Identified"));
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

    private static void WireEngageAgent(SimulationSession session, string shooterId, AutonomyLevel autonomy)
    {
        var unit = new UnitTarget(new TargetId(shooterId));
        var agent = session.Orchestrator.CreateAgent(
            new AgentId($"agent-{shooterId}"),
            PersonalityCatalog.All[0].Traits,
            autonomy,
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

    internal static Definition LoadDefaultDefinition()
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
