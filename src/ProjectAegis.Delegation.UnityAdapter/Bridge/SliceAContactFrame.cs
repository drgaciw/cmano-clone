namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using System.Collections.ObjectModel;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Delegation.SensorToShooter;
using ProjectAegis.Delegation.Skills;

/// <summary>Tick-level read model for Slice A; no live simulation handles (ADR-010 §2–3, ADR-007, ADR-001).</summary>
public sealed record SliceAContactFrame(
    KillChainContactSnapshot KillChain,
    ContactProvenanceSnapshot Provenance,
    SensorToShooterSnapshot Chains,
    IReadOnlyList<ContactPictureEntry> Contacts,
    IReadOnlyDictionary<string, C2AuthorityProjection> Authorities,
    bool EligibilityAvailable = false,
    ulong SimTick = 0)
{
    /// <summary>No received simulation frame.</summary>
    public static SliceAContactFrame Empty { get; } = new(
        KillChainContactSnapshot.Empty, ContactProvenanceSnapshot.Empty, SensorToShooterSnapshot.Empty,
        Array.Empty<ContactPictureEntry>(),
        new ReadOnlyDictionary<string, C2AuthorityProjection>(new Dictionary<string, C2AuthorityProjection>(StringComparer.Ordinal)));
}

/// <summary>Optional authoritative, actor-specific authority facts attached to the current world snapshot.</summary>
public interface ISliceAContactAuthoritySource
{
    /// <summary>Returns current authority evidence for precisely this contact and nominated shooter.</summary>
    bool TryGetAuthorityContext(string contactId, string shooterUnitId, out C2AuthorityProjectionContext context);
}

/// <summary>Read-only composition after a simulation tick; never primes engagement state or refills magazines.</summary>
public static class SliceAContactFrameBridge
{
    /// <summary>
    /// Builds cached projections. Without a supplied authoritative shooter source, eligibility fails closed:
    /// ISimWorldSnapshot has no per-shooter side, current geometry or commitment facts. Scenario defaults
    /// and historical EngageWorld contexts are not evidence of present shooter eligibility.
    /// </summary>
    public static SliceAContactFrame Build(
        ISimWorldSnapshot snapshot,
        DelegationBridge bridge,
        ICatalogReader? catalog = null,
        ISensorToShooterShooterSource? shooters = null)
    {
        if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));
        if (bridge is null) throw new ArgumentNullException(nameof(bridge));

        var log = bridge.Orchestrator.DecisionLog;
        var tick = snapshot.SimTime <= 0 ? 0UL : (ulong)snapshot.SimTime;
        var killChain = KillChainContactStateBridge.Build(snapshot, log);
        // ProjectWithBda indexes by target and cannot represent two observers of the same target.
        // Kill-chain already folds BDA per contact, including nonterminal degradation.
        var active = ContactPictureProjection.Project(log).ToDictionary(c => c.ContactId, StringComparer.Ordinal);
        var contacts = Array.AsReadOnly(killChain.Contacts.Select(c =>
            c.Loss is KillChainLossKind.Lost or KillChainLossKind.DegradedL1 or KillChainLossKind.DegradedL2
                ? new ContactPictureEntry(c.ContactId, c.TargetId, c.ObserverId,
                    c.Loss == KillChainLossKind.Lost ? BdaContactDamageStates.Lost
                        : c.Loss == KillChainLossKind.DegradedL1 ? BdaContactDamageStates.DegradedL1 : BdaContactDamageStates.DegradedL2,
                    c.LastSimTick, c.LastSimTime)
                : active[c.ContactId]).ToArray());
        var provenance = ContactProvenanceProjection.Project(contacts, tick,
            CommsStateProjection.Project(log).State, catalog, bridge.Orchestrator.ScenarioPolicy?.CommsDisplay);
        var source = shooters ?? snapshot as ISensorToShooterShooterSource;
        var chains = SensorToShooterProjection.Project(killChain,
            source == null ? null : new LiveCandidateGuard(source, snapshot, bridge), catalog);
        var authorities = new Dictionary<string, C2AuthorityProjection>(StringComparer.Ordinal);
        foreach (var chain in chains.Chains)
        {
            var shooter = chain.Links.FirstOrDefault(l => l.Kind == SensorToShooterLinkKind.EligibleShooter && l.IsLinked)?.UnitId;
            var registered = !string.IsNullOrEmpty(shooter)
                && bridge.Registry.TryGetBinding(new TargetId(shooter!), out _);
            if (!registered || snapshot is not ISliceAContactAuthoritySource authoritySource
                || !authoritySource.TryGetAuthorityContext(chain.ContactId, shooter!, out var supplied)
                || supplied.TrackSource == TrackSource.Unknown)
            {
                continue;
            }
            var quality = provenance.Contacts.FirstOrDefault(p => string.Equals(p.ContactId, chain.ContactId, StringComparison.Ordinal));
            var currentTrack = quality != null && quality.Freshness == ContactProvenanceFreshness.Fresh
                && !quality.OutOfCommsUnknown;
            var context = supplied with
            {
                Lane = SkillLane.Propose,
                RequiredApproval = RequiredApproval.WeaponsRelease,
                FireControlSatisfied = supplied.FireControlSatisfied && chain.IsComplete && currentTrack,
                CommandId = "engage",
            };
            authorities.Add(chain.ContactId, C2AuthorityProjector.Project(context));
        }

        return new SliceAContactFrame(killChain, provenance, chains, contacts,
            new ReadOnlyDictionary<string, C2AuthorityProjection>(authorities), source != null, tick);
    }

    private sealed class LiveCandidateGuard(
        ISensorToShooterShooterSource source, ISimWorldSnapshot snapshot, DelegationBridge bridge)
        : ISensorToShooterShooterSource
    {
        public IReadOnlyList<SensorToShooterShooterCandidate> GetCandidatesForTarget(string targetId)
        {
            var result = new List<SensorToShooterShooterCandidate>();
            foreach (var candidate in source.GetCandidatesForTarget(targetId))
            {
                var unit = new TargetId(candidate.ShooterUnitId);
                if (candidate.ShooterUnitId == targetId || !snapshot.IsMemberAlive(unit)
                    || !bridge.Registry.TryGetBinding(unit, out _)
                    || bridge.Session?.UnitReadiness?.IsReadyForLaunch(candidate.ShooterUnitId) == false)
                {
                    continue;
                }

                var rounds = candidate.RoundsRemaining;
                if (bridge.Session?.Magazines is { } ledger)
                {
                    // Missing ledger entry is not permission to seed/refill from configuration.
                    rounds = ledger.TryGetRounds(OrderActionMapper.TargetIdToUlong(unit), 0, out var tracked) ? tracked : 0;
                }

                result.Add(candidate with { RoundsRemaining = rounds });
            }

            return result;
        }
    }
}
