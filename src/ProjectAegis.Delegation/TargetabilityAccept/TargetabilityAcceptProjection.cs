namespace ProjectAegis.Delegation.TargetabilityAccept;

using Data.Catalog;
using Data.Scenario.Authoring;
using Decision;
using Projection;
using SensorToShooter;
using Skills;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

/// <summary>
/// DRG-219: composes wave-1 provenance, sensor-to-shooter, and authority projectors into one
/// replay-stable acceptance snapshot. Headless only — no Unity or tick-path mutation.
/// </summary>
public static class TargetabilityAcceptProjection
{
    public static TargetabilityAcceptSnapshot Project(
        DecisionLog? log,
        ulong currentSimTick,
        in C2AuthorityProjectionContext authorityContext,
        IKillChainFireControlSource? fireControl = null,
        ISensorToShooterShooterSource? shooters = null,
        ICatalogReader? catalog = null,
        string weaponId = CatalogWeaponIds.MvpDefault,
        int staleThresholdTicks = ContactProvenanceProjection.DefaultStaleThresholdTicks,
        IReadOnlyList<ScenarioOrbatUnitDto>? orbatUnits = null,
        ScenarioCommsDisplaySettings? commsDisplay = null)
    {
        if (log is null)
        {
            return TargetabilityAcceptSnapshot.Empty;
        }

        var provenance = ContactProvenanceProjection.Project(
            log,
            currentSimTick,
            catalog,
            commsDisplay: commsDisplay,
            staleThresholdTicks: staleThresholdTicks,
            orbatUnits: orbatUnits);
        // SensorToShooterProjection has no commsDisplay parameter — provenance owns degraded stale divisor.
        var sensorToShooter = SensorToShooterProjection.Project(
            log,
            currentSimTick,
            fireControl,
            shooters,
            catalog,
            weaponId,
            staleThresholdTicks);
        var authority = C2AuthorityProjector.Project(in authorityContext);

        return Project(provenance, sensorToShooter, authority);
    }

    public static TargetabilityAcceptSnapshot Project(
        ContactProvenanceSnapshot? provenance,
        SensorToShooterSnapshot? sensorToShooter,
        C2AuthorityProjection? authority)
    {
        if (authority is null)
        {
            return TargetabilityAcceptSnapshot.Empty;
        }

        var provenanceByContact = IndexProvenance(provenance);
        var chainsByContact = IndexChains(sensorToShooter);
        var contactIds = CollectContactIds(provenanceByContact, chainsByContact);
        if (contactIds.Count == 0)
        {
            return TargetabilityAcceptSnapshot.Empty;
        }

        var rows = new TargetabilityAcceptContactRow[contactIds.Count];
        for (var i = 0; i < contactIds.Count; i++)
        {
            var contactId = contactIds[i];
            provenanceByContact.TryGetValue(contactId, out var provenanceRow);
            chainsByContact.TryGetValue(contactId, out var chain);
            var (disposition, causeCode) = ResolveDisposition(provenanceRow, chain, authority);
            rows[i] = new TargetabilityAcceptContactRow(
                contactId,
                ResolveTargetId(provenanceRow, chain),
                disposition,
                causeCode,
                provenanceRow,
                chain,
                authority);
        }

        return new TargetabilityAcceptSnapshot(rows);
    }

    /// <summary>
    /// Composes cached Slice A facts with the authority already resolved for each contact's nominated shooter.
    /// A known contact-wide track refusal is kept when no eligible shooter is linked and the authority
    /// dictionary has no entry. Permission and doctrine still require that contact's actor authority;
    /// another contact's authority is never copied.
    /// </summary>
    public static TargetabilityAcceptSnapshot ProjectPerContact(
        ContactProvenanceSnapshot? provenance,
        SensorToShooterSnapshot? sensorToShooter,
        IReadOnlyDictionary<string, C2AuthorityProjection> authorities)
    {
        if (authorities is null) throw new ArgumentNullException(nameof(authorities));
        var provenanceByContact = IndexProvenance(provenance);
        var chainsByContact = IndexChains(sensorToShooter);
        var rows = new List<TargetabilityAcceptContactRow>();
        foreach (var contactId in CollectContactIds(provenanceByContact, chainsByContact))
        {
            provenanceByContact.TryGetValue(contactId, out var provenanceRow);
            chainsByContact.TryGetValue(contactId, out var chain);
            if (!authorities.TryGetValue(contactId, out var authority))
            {
                if (!TryResolveUnscopedTrackRefusal(provenanceRow, chain, out var refusalCause))
                {
                    continue;
                }

                rows.Add(new TargetabilityAcceptContactRow(
                    contactId,
                    ResolveTargetId(provenanceRow, chain),
                    TargetabilityAcceptDisposition.Withheld,
                    refusalCause,
                    provenanceRow,
                    chain,
                    NoActorAuthority));
                continue;
            }

            var (disposition, causeCode) = ResolveDisposition(provenanceRow, chain, authority);
            rows.Add(new TargetabilityAcceptContactRow(contactId, ResolveTargetId(provenanceRow, chain),
                disposition, causeCode, provenanceRow, chain, authority));
        }

        return new TargetabilityAcceptSnapshot(rows.AsReadOnly());
    }

    /// <summary>
    /// Fail-closed stand-in used only when no actor was resolved. It does not permit engage and is not a
    /// nominated shooter's doctrine; contact-wide consumers key off disposition, cause, and a null shooter id.
    /// </summary>
    private static readonly C2AuthorityProjection NoActorAuthority = new(
        new RoeProjection(
            RoeLevel.WeaponsFree,
            RoeProjection.FormatRoeLabel(RoeLevel.WeaponsFree),
            C2AuthorityDisposition.Withheld,
            null,
            false),
        new C2TargetingAuthority(C2AuthorityDisposition.Withheld, null, null),
        Array.Empty<C2AuthorityActionState>());

    /// <summary>
    /// Track refusals that do not need an actor: missing or stale provenance, or a broken chain with no
    /// affirmatively linked eligible shooter. A linked shooter still requires that actor's authority.
    /// </summary>
    private static bool TryResolveUnscopedTrackRefusal(
        ContactProvenanceState? provenance,
        SensorToShooterChain? chain,
        out string causeCode)
    {
        causeCode = TargetabilityAcceptCauseCodes.None;
        if (HasLinkedEligibleShooter(chain))
        {
            return false;
        }

        if (provenance is null)
        {
            causeCode = TargetabilityAcceptCauseCodes.MissingProvenance;
            return true;
        }

        if (provenance.QualityState.HasFlag(ContactProvenanceQualityState.CatalogMiss))
        {
            causeCode = TargetabilityAcceptCauseCodes.CatalogMiss;
            return true;
        }

        if (provenance.Freshness == ContactProvenanceFreshness.Stale)
        {
            causeCode = TargetabilityAcceptCauseCodes.Stale;
            return true;
        }

        if (provenance.QualityState.HasFlag(ContactProvenanceQualityState.SilentComms)
            && provenance.OutOfCommsUnknown)
        {
            causeCode = TargetabilityAcceptCauseCodes.SilentComms;
            return true;
        }

        if (chain is null || !chain.IsComplete)
        {
            causeCode = FormatSensorToShooterCause(chain?.PrimaryBreakCause ?? SensorToShooterBreakCause.StaleTrack);
            return true;
        }

        return false;
    }

    private static bool HasLinkedEligibleShooter(SensorToShooterChain? chain)
    {
        var links = chain?.Links;
        if (links is null)
        {
            return false;
        }

        for (var i = 0; i < links.Count; i++)
        {
            var link = links[i];
            if (link.Kind == SensorToShooterLinkKind.EligibleShooter
                && link.IsLinked
                && !string.IsNullOrWhiteSpace(link.UnitId))
            {
                return true;
            }
        }

        return false;
    }

    internal static (TargetabilityAcceptDisposition Disposition, string CauseCode) ResolveDisposition(
        ContactProvenanceState? provenance,
        SensorToShooterChain? chain,
        C2AuthorityProjection authority)
    {
        // Fail closed: chain-only rows without provenance must never become Permitted.
        if (provenance is null)
        {
            return (TargetabilityAcceptDisposition.Withheld, TargetabilityAcceptCauseCodes.MissingProvenance);
        }

        if (provenance.QualityState.HasFlag(ContactProvenanceQualityState.CatalogMiss))
        {
            return (TargetabilityAcceptDisposition.Withheld, TargetabilityAcceptCauseCodes.CatalogMiss);
        }

        if (provenance.Freshness == ContactProvenanceFreshness.Stale)
        {
            return (TargetabilityAcceptDisposition.Withheld, TargetabilityAcceptCauseCodes.Stale);
        }

        if (provenance.QualityState.HasFlag(ContactProvenanceQualityState.SilentComms)
            && provenance.OutOfCommsUnknown)
        {
            return (TargetabilityAcceptDisposition.Withheld, TargetabilityAcceptCauseCodes.SilentComms);
        }

        if (chain is null || !chain.IsComplete)
        {
            return (
                TargetabilityAcceptDisposition.Withheld,
                FormatSensorToShooterCause(chain?.PrimaryBreakCause ?? SensorToShooterBreakCause.StaleTrack));
        }

        if (authority.Targeting.Disposition == C2AuthorityDisposition.Withheld)
        {
            return (
                TargetabilityAcceptDisposition.Withheld,
                authority.Targeting.ReasonCode ?? TargetabilityAcceptCauseCodes.ApprovalRequired);
        }

        if (authority.Targeting.Disposition == C2AuthorityDisposition.ApprovalRequired)
        {
            return (
                TargetabilityAcceptDisposition.Withheld,
                authority.Targeting.ReasonCode ?? TargetabilityAcceptCauseCodes.ApprovalRequired);
        }

        return (TargetabilityAcceptDisposition.Permitted, TargetabilityAcceptCauseCodes.None);
    }

    internal static string FormatSensorToShooterCause(SensorToShooterBreakCause cause) =>
        cause switch
        {
            SensorToShooterBreakCause.LostSensor => TargetabilityAcceptCauseCodes.LostSensor,
            SensorToShooterBreakCause.StaleTrack => TargetabilityAcceptCauseCodes.StaleTrack,
            SensorToShooterBreakCause.NoFireControl => TargetabilityAcceptCauseCodes.NoFireControl,
            SensorToShooterBreakCause.NoEligibleShooter => TargetabilityAcceptCauseCodes.NoEligibleShooter,
            SensorToShooterBreakCause.DegradedTrack => TargetabilityAcceptCauseCodes.DegradedTrack,
            _ => TargetabilityAcceptCauseCodes.StaleTrack,
        };

    private static Dictionary<string, ContactProvenanceState> IndexProvenance(
        ContactProvenanceSnapshot? provenance)
    {
        var map = new Dictionary<string, ContactProvenanceState>(StringComparer.Ordinal);
        if (provenance is null)
        {
            return map;
        }

        for (var i = 0; i < provenance.Contacts.Count; i++)
        {
            var row = provenance.Contacts[i];
            map[row.ContactId] = row;
        }

        return map;
    }

    private static Dictionary<string, SensorToShooterChain> IndexChains(
        SensorToShooterSnapshot? sensorToShooter)
    {
        var map = new Dictionary<string, SensorToShooterChain>(StringComparer.Ordinal);
        if (sensorToShooter is null)
        {
            return map;
        }

        for (var i = 0; i < sensorToShooter.Chains.Count; i++)
        {
            var chain = sensorToShooter.Chains[i];
            map[chain.ContactId] = chain;
        }

        return map;
    }

    private static List<string> CollectContactIds(
        IReadOnlyDictionary<string, ContactProvenanceState> provenanceByContact,
        IReadOnlyDictionary<string, SensorToShooterChain> chainsByContact)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var contactId in provenanceByContact.Keys)
        {
            ids.Add(contactId);
        }

        foreach (var contactId in chainsByContact.Keys)
        {
            ids.Add(contactId);
        }

        return ids.OrderBy(id => id, StringComparer.Ordinal).ToList();
    }

    private static string ResolveTargetId(
        ContactProvenanceState? provenance,
        SensorToShooterChain? chain) =>
        provenance?.Source.TargetId
        ?? chain?.TargetId
        ?? string.Empty;
}
