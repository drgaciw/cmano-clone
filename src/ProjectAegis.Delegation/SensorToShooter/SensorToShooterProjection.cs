namespace ProjectAegis.Delegation.SensorToShooter;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.TrackCustody;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Scenario;

/// <summary>
/// DRG-207: projects kill-chain contact state plus catalog engage envelope into an inspectable
/// sensor → track → targetability → eligible-shooter chain. Sim/order-log truth only.
/// </summary>
public static class SensorToShooterProjection
{
    public static SensorToShooterSnapshot Project(
        DecisionLog? log,
        ulong currentSimTick,
        IKillChainFireControlSource? fireControl = null,
        ISensorToShooterShooterSource? shooters = null,
        ICatalogReader? catalog = null,
        string weaponId = CatalogWeaponIds.MvpDefault,
        int staleThresholdTicks = KillChainContactStateProjection.DefaultStaleThresholdTicks,
        int dropThresholdTicks = KillChainContactStateProjection.DefaultDropThresholdTicks)
    {
        if (log is null)
        {
            return SensorToShooterSnapshot.Empty;
        }

        var killChain = KillChainContactStateProjection.Project(
            log,
            currentSimTick,
            fireControl,
            staleThresholdTicks,
            dropThresholdTicks);

        var custody = TrackCustodyProjection.Project(
            log,
            currentSimTick,
            fireControl,
            catalog,
            staleThresholdTicks: staleThresholdTicks,
            dropThresholdTicks: dropThresholdTicks);

        return Project(killChain, shooters, catalog, weaponId, custody, log);
    }

    public static SensorToShooterSnapshot Project(
        KillChainContactSnapshot? killChain,
        ISensorToShooterShooterSource? shooters = null,
        ICatalogReader? catalog = null,
        string weaponId = CatalogWeaponIds.MvpDefault,
        TrackCustodySnapshot? custody = null,
        DecisionLog? log = null)
    {
        if (killChain is null || killChain.Contacts.Count == 0)
        {
            return SensorToShooterSnapshot.Empty;
        }

        var custodyByContact = custody?.Rows.ToDictionary(r => r.ContactId, StringComparer.Ordinal);
        var chains = new List<SensorToShooterChain>(killChain.Contacts.Count);
        foreach (var contact in killChain.Contacts.OrderBy(c => c.ContactId, StringComparer.Ordinal))
        {
            var custodyRow = custodyByContact?.GetValueOrDefault(contact.ContactId);
            chains.Add(BuildChain(contact, shooters, catalog, weaponId, custodyRow, log));
        }

        return new SensorToShooterSnapshot(chains);
    }

    /// <summary>Replay-stable canonical form; invariant culture; ordinal sorts only.</summary>
    public static string ComputeFingerprint(SensorToShooterSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.Chains.Count == 0)
        {
            return "sts:empty";
        }

        var builder = new System.Text.StringBuilder();
        builder.Append("sts:c=");
        builder.Append(snapshot.Chains.Count);
        for (var i = 0; i < snapshot.Chains.Count; i++)
        {
            var chain = snapshot.Chains[i];
            builder.Append('|');
            builder.Append(chain.ContactId);
            builder.Append(',');
            builder.Append(chain.TargetId);
            builder.Append(',');
            builder.Append(chain.ObserverId);
            builder.Append(',');
            builder.Append(chain.IsComplete ? '1' : '0');
            builder.Append(',');
            builder.Append((int)chain.PrimaryBreakCause);
            builder.Append(',');
            builder.Append((int)chain.TrackCustodyBreakdown);
            builder.Append(',');
            builder.Append(chain.Links.Count);
            for (var j = 0; j < chain.Links.Count; j++)
            {
                var link = chain.Links[j];
                builder.Append(';');
                builder.Append((int)link.Kind);
                builder.Append(',');
                builder.Append(link.IsLinked ? '1' : '0');
                builder.Append(',');
                builder.Append((int)link.BreakCause);
                builder.Append(',');
                builder.Append((int)link.TrackCustodyBreakdown);
                builder.Append(',');
                builder.Append(link.UnitId ?? string.Empty);
                builder.Append(',');
                builder.Append(link.Detail ?? string.Empty);
            }
        }

        return builder.ToString();
    }

    private static SensorToShooterChain BuildChain(
        KillChainContactState contact,
        ISensorToShooterShooterSource? shooters,
        ICatalogReader? catalog,
        string weaponId,
        TrackCustodyRow? custodyRow = null,
        DecisionLog? log = null)
    {
        var breakdown = ResolveChainBreakdown(contact, custodyRow, log);

        var sensorLink = BuildSensorLink(contact, breakdown);
        var trackLink = BuildTrackLink(contact, breakdown);
        var targetabilityLink = BuildTargetabilityLink(contact, breakdown);
        var shooterLink = BuildEligibleShooterLink(
            contact,
            targetabilityLink,
            shooters,
            catalog,
            weaponId,
            breakdown);

        var links = new[]
        {
            sensorLink,
            trackLink,
            targetabilityLink,
            shooterLink,
        };

        var primaryBreak = SensorToShooterBreakCause.None;
        for (var i = 0; i < links.Length; i++)
        {
            if (!links[i].IsLinked)
            {
                primaryBreak = links[i].BreakCause;
                break;
            }
        }

        var isComplete = primaryBreak == SensorToShooterBreakCause.None;
        return new SensorToShooterChain(
            contact.ContactId,
            contact.TargetId,
            contact.ObserverId,
            isComplete,
            primaryBreak,
            links,
            breakdown);
    }

    private static TrackCustodyBreakdown ResolveChainBreakdown(
        KillChainContactState contact,
        TrackCustodyRow? custodyRow,
        DecisionLog? log)
    {
        if (custodyRow is not null && custodyRow.Breakdown != TrackCustodyBreakdown.None)
        {
            return custodyRow.Breakdown;
        }

        var custody = contact.Loss == KillChainLossKind.Lost ? TrackCustodyState.Dropped : TrackCustodyState.Held;
        var cause = contact.Loss switch
        {
            KillChainLossKind.Lost => TrackCustodyCause.LostSensor,
            KillChainLossKind.Stale => TrackCustodyCause.Stale,
            _ => TrackCustodyCause.None,
        };

        var commsState = CommsState.Nominal;
        if (log is not null)
        {
            var commsSnapshot = CommsStateProjection.Project(log);
            commsState = commsSnapshot.State;
        }

        return TrackCustodyProjection.ResolveBreakdown(contact, null, commsState, custody, cause, log);
    }

    private static SensorToShooterChainLink BuildSensorLink(KillChainContactState contact, TrackCustodyBreakdown breakdown)
    {
        if (contact.Loss == KillChainLossKind.Lost)
        {
            return BrokenLink(
                SensorToShooterLinkKind.Sensor,
                SensorToShooterBreakCause.LostSensor,
                contact.ObserverId,
                contact,
                SensorToShooterBreakCauseLabels.LostSensor,
                breakdown);
        }

        if (!contact.DetectionCaptured)
        {
            return BrokenLink(
                SensorToShooterLinkKind.Sensor,
                SensorToShooterBreakCause.LostSensor,
                contact.ObserverId,
                contact,
                "sensor not detecting",
                breakdown);
        }

        return Linked(
            SensorToShooterLinkKind.Sensor,
            contact.ObserverId,
            contact,
            $"sensor:{contact.ObserverId}");
    }

    private static SensorToShooterChainLink BuildTrackLink(KillChainContactState contact, TrackCustodyBreakdown breakdown)
    {
        if (contact.Loss == KillChainLossKind.Lost)
        {
            return BrokenLink(
                SensorToShooterLinkKind.Track,
                SensorToShooterBreakCause.LostSensor,
                contact.ContactId,
                contact,
                SensorToShooterBreakCauseLabels.LostSensor,
                breakdown);
        }

        if (contact.Loss == KillChainLossKind.Stale)
        {
            return BrokenLink(
                SensorToShooterLinkKind.Track,
                SensorToShooterBreakCause.StaleTrack,
                contact.ContactId,
                contact,
                SensorToShooterBreakCauseLabels.StaleTrack,
                breakdown);
        }

        if (!contact.TrackContinuous)
        {
            return BrokenLink(
                SensorToShooterLinkKind.Track,
                SensorToShooterBreakCause.StaleTrack,
                contact.ContactId,
                contact,
                SensorToShooterBreakCauseLabels.StaleTrack,
                breakdown);
        }

        return Linked(
            SensorToShooterLinkKind.Track,
            contact.ContactId,
            contact,
            $"track:{contact.ContactId}");
    }

    private static SensorToShooterChainLink BuildTargetabilityLink(KillChainContactState contact, TrackCustodyBreakdown breakdown)
    {
        if (contact.Loss == KillChainLossKind.Lost)
        {
            return BrokenLink(
                SensorToShooterLinkKind.Targetability,
                SensorToShooterBreakCause.LostSensor,
                contact.ContactId,
                contact,
                SensorToShooterBreakCauseLabels.LostSensor,
                breakdown);
        }

        if (contact.Loss == KillChainLossKind.Stale)
        {
            return BrokenLink(
                SensorToShooterLinkKind.Targetability,
                SensorToShooterBreakCause.StaleTrack,
                contact.ContactId,
                contact,
                SensorToShooterBreakCauseLabels.StaleTrack,
                breakdown);
        }

        if (contact.Loss is KillChainLossKind.DegradedL1 or KillChainLossKind.DegradedL2)
        {
            return BrokenLink(
                SensorToShooterLinkKind.Targetability,
                SensorToShooterBreakCause.DegradedTrack,
                contact.ContactId,
                contact,
                SensorToShooterBreakCauseLabels.DegradedTrack,
                breakdown);
        }

        if (!contact.Targetable)
        {
            var cause = contact.TrackContinuous && contact.Loss == KillChainLossKind.None
                ? SensorToShooterBreakCause.NoFireControl
                : SensorToShooterBreakCause.StaleTrack;
            var label = cause == SensorToShooterBreakCause.NoFireControl
                ? SensorToShooterBreakCauseLabels.NoFireControl
                : SensorToShooterBreakCauseLabels.StaleTrack;
            return BrokenLink(
                SensorToShooterLinkKind.Targetability,
                cause,
                contact.ContactId,
                contact,
                label,
                cause == SensorToShooterBreakCause.NoFireControl ? TrackCustodyBreakdown.None : breakdown);
        }

        return Linked(
            SensorToShooterLinkKind.Targetability,
            contact.ContactId,
            contact,
            "targetable");
    }

    private static SensorToShooterChainLink BuildEligibleShooterLink(
        KillChainContactState contact,
        SensorToShooterChainLink targetabilityLink,
        ISensorToShooterShooterSource? shooters,
        ICatalogReader? catalog,
        string weaponId,
        TrackCustodyBreakdown breakdown)
    {
        if (!targetabilityLink.IsLinked)
        {
            return BrokenLink(
                SensorToShooterLinkKind.EligibleShooter,
                targetabilityLink.BreakCause,
                null,
                contact,
                targetabilityLink.Detail ?? targetabilityLink.CauseLabel,
                targetabilityLink.TrackCustodyBreakdown);
        }

        var candidates = shooters?.GetCandidatesForTarget(contact.TargetId) ?? Array.Empty<SensorToShooterShooterCandidate>();
        if (candidates.Count == 0)
        {
            return BrokenLink(
                SensorToShooterLinkKind.EligibleShooter,
                SensorToShooterBreakCause.NoEligibleShooter,
                null,
                contact,
                SensorToShooterBreakCauseLabels.NoEligibleShooter,
                TrackCustodyBreakdown.None);
        }

        var ordered = candidates
            .OrderBy(c => c.ShooterUnitId, StringComparer.Ordinal)
            .ToArray();

        string? eligibleShooter = null;
        string? abortDetail = null;
        for (var i = 0; i < ordered.Length; i++)
        {
            var candidate = ordered[i];
            var engageCtx = CatalogEngageEnvelope.Apply(
                candidate.EngageDefaults.ToEngageContext(candidate.RoundsRemaining),
                catalog,
                weaponId);
            if (!HasSufficientAmmo(in engageCtx, candidate.RoundsRemaining))
            {
                abortDetail = "NO_AMMO";
                continue;
            }

            var preview = EngagePreviewProjection.Project(
                in engageCtx,
                candidate.EngageDefaults.DlzPersonality);
            if (preview.CanFire)
            {
                eligibleShooter = candidate.ShooterUnitId;
                break;
            }

            abortDetail = preview.AbortPreviewCode ?? "engage blocked";
        }

        if (eligibleShooter is null)
        {
            return BrokenLink(
                SensorToShooterLinkKind.EligibleShooter,
                SensorToShooterBreakCause.NoEligibleShooter,
                null,
                contact,
                abortDetail ?? SensorToShooterBreakCauseLabels.NoEligibleShooter,
                TrackCustodyBreakdown.None);
        }

        return Linked(
            SensorToShooterLinkKind.EligibleShooter,
            eligibleShooter,
            contact,
            $"shooter:{eligibleShooter}");
    }

    private static SensorToShooterChainLink Linked(
        SensorToShooterLinkKind kind,
        string? unitId,
        KillChainContactState contact,
        string detail) =>
        new(
            kind,
            IsLinked: true,
            BreakCause: SensorToShooterBreakCause.None,
            UnitId: unitId,
            ContactId: contact.ContactId,
            TargetId: contact.TargetId,
            Detail: detail,
            TrackCustodyBreakdown: TrackCustodyBreakdown.None);

    private static bool HasSufficientAmmo(in EngageContext ctx, int roundsRemaining)
    {
        if (roundsRemaining <= 0)
        {
            return false;
        }

        var salvoSize = Math.Max(1, ctx.SalvoSize);
        return roundsRemaining >= salvoSize;
    }

    private static SensorToShooterChainLink BrokenLink(
        SensorToShooterLinkKind kind,
        SensorToShooterBreakCause cause,
        string? unitId,
        KillChainContactState contact,
        string detail,
        TrackCustodyBreakdown breakdown = TrackCustodyBreakdown.None) =>
        new(
            kind,
            IsLinked: false,
            BreakCause: cause,
            UnitId: unitId,
            ContactId: contact.ContactId,
            TargetId: contact.TargetId,
            Detail: detail,
            TrackCustodyBreakdown: breakdown);
}
