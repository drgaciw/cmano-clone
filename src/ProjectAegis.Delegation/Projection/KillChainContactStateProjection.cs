namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Decision;

/// <summary>
/// DRG-179: projects order-log contact (and BDA) rows into deterministic Find / Fix / Track /
/// Target transitions plus detection, location sufficiency, track continuity, targetability,
/// and loss/degradation. Sim-clock only. No UI-derived truth.
/// </summary>
public static class KillChainContactStateProjection
{
    /// <summary>Matches <c>ScenarioContactLifecycle.Default.StaleThresholdTicks</c> / sensor GDD.</summary>
    public const int DefaultStaleThresholdTicks = 30;

    /// <summary>Sensor GDD drop threshold (not on <c>ScenarioContactLifecycle</c> yet).</summary>
    public const int DefaultDropThresholdTicks = 120;

    public static KillChainContactSnapshot Project(
        DecisionLog? log,
        ulong currentSimTick,
        IKillChainFireControlSource? fireControl = null,
        int staleThresholdTicks = DefaultStaleThresholdTicks,
        int dropThresholdTicks = DefaultDropThresholdTicks)
    {
        if (log is null)
        {
            return KillChainContactSnapshot.Empty;
        }

        var picture = ContactPictureProjection.Project(log);
        var contactsByTarget = BuildContactsByTarget(picture);
        var byTarget = BuildRepresentativeContactsByTarget(contactsByTarget);

        var bdaPerTarget = OrderLogBdaProjection.ProjectBdaContactChanges(log, byTarget);
        var bda = FanOutBdaContactChanges(bdaPerTarget, contactsByTarget);
        if (bda.Count == 0)
        {
            return Project(log.ContactChanges, currentSimTick, fireControl, staleThresholdTicks, dropThresholdTicks);
        }

        var sensor = log.ContactChanges;
        var merged = new ContactChangeRecord[sensor.Count + bda.Count];
        for (var i = 0; i < sensor.Count; i++)
        {
            merged[i] = sensor[i];
        }

        for (var i = 0; i < bda.Count; i++)
        {
            merged[sensor.Count + i] = bda[i];
        }

        return Project(merged, currentSimTick, fireControl, staleThresholdTicks, dropThresholdTicks);
    }

    public static KillChainContactSnapshot Project(
        IReadOnlyList<ContactChangeRecord>? changes,
        ulong currentSimTick,
        IKillChainFireControlSource? fireControl = null,
        int staleThresholdTicks = DefaultStaleThresholdTicks,
        int dropThresholdTicks = DefaultDropThresholdTicks)
    {
        if (changes is null || changes.Count == 0)
        {
            return KillChainContactSnapshot.Empty;
        }

        var staleTicks = Math.Max(1, staleThresholdTicks);
        var dropTicks = Math.Max(staleTicks, dropThresholdTicks);
        var ordered = changes
            .OrderBy(c => c.SimTick)
            .ThenBy(c => c.SequenceId)
            .ThenBy(c => c.ContactId, StringComparer.Ordinal)
            .ToArray();

        var tracks = new Dictionary<string, TrackAcc>(StringComparer.Ordinal);
        var transitions = new List<KillChainContactTransition>();

        for (var i = 0; i < ordered.Length; i++)
        {
            var change = ordered[i];
            if (string.IsNullOrEmpty(change.ContactId))
            {
                continue;
            }

            ApplyChange(tracks, change, fireControl, transitions);
        }

        var freshnessOrder = tracks.Keys.OrderBy(id => id, StringComparer.Ordinal).ToArray();
        for (var i = 0; i < freshnessOrder.Length; i++)
        {
            ApplyFreshness(tracks[freshnessOrder[i]], currentSimTick, fireControl, staleTicks, dropTicks, transitions);
        }

        var contacts = tracks.Values
            .OrderBy(t => t.ContactId, StringComparer.Ordinal)
            .Select(t => t.ToState())
            .ToArray();

        var published = transitions
            .OrderBy(t => t.SimTick)
            .ThenBy(t => t.CorrelationSequenceId)
            .ThenBy(t => (int)t.Kind)
            .ThenBy(t => t.ContactId, StringComparer.Ordinal)
            .ToArray();

        return new KillChainContactSnapshot(contacts, published);
    }

    /// <summary>
    /// Replay-stable canonical form: same log + tick + fire-control facts yield the same string.
    /// Invariant culture; no wall clock.
    /// </summary>
    public static string ComputeFingerprint(KillChainContactSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.Contacts.Count == 0 && snapshot.Transitions.Count == 0)
        {
            return "kc:empty";
        }

        var builder = new System.Text.StringBuilder();
        builder.Append("kc:c=");
        builder.Append(snapshot.Contacts.Count);
        for (var i = 0; i < snapshot.Contacts.Count; i++)
        {
            var c = snapshot.Contacts[i];
            builder.Append('|');
            builder.Append(c.ContactId);
            builder.Append(',');
            builder.Append(c.TargetId);
            builder.Append(',');
            builder.Append(c.ObserverId);
            builder.Append(',');
            builder.Append((int)c.Phase);
            builder.Append(',');
            builder.Append((int)c.Loss);
            builder.Append(',');
            builder.Append(c.DetectionCaptured ? '1' : '0');
            builder.Append(c.LocationSufficient ? '1' : '0');
            builder.Append(c.TrackContinuous ? '1' : '0');
            builder.Append(c.Targetable ? '1' : '0');
            builder.Append(',');
            builder.Append(c.FirstSimTick);
            builder.Append(',');
            builder.Append(c.FirstSimTime.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(c.LastSimTick);
            builder.Append(',');
            builder.Append(c.LastSimTime.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(c.CorrelationSequenceId);
            builder.Append(',');
            AppendJoined(builder, c.SourceSequenceIds);
            builder.Append(',');
            AppendJoined(builder, c.SourceRefs);
        }

        builder.Append("#t=");
        builder.Append(snapshot.Transitions.Count);
        for (var i = 0; i < snapshot.Transitions.Count; i++)
        {
            var t = snapshot.Transitions[i];
            builder.Append('|');
            builder.Append((int)t.Kind);
            builder.Append(',');
            builder.Append(t.ContactId);
            builder.Append(',');
            builder.Append((int)t.PreviousPhase);
            builder.Append(',');
            builder.Append((int)t.NewPhase);
            builder.Append(',');
            builder.Append((int)t.Loss);
            builder.Append(',');
            builder.Append(t.SimTick);
            builder.Append(',');
            builder.Append(t.SimTime.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(t.CorrelationSequenceId);
        }

        return builder.ToString();
    }

    private static Dictionary<string, List<ContactPictureEntry>> BuildContactsByTarget(
        IReadOnlyList<ContactPictureEntry> picture)
    {
        var contactsByTarget = new Dictionary<string, List<ContactPictureEntry>>(picture.Count, StringComparer.Ordinal);
        for (var i = 0; i < picture.Count; i++)
        {
            var entry = picture[i];
            if (!contactsByTarget.TryGetValue(entry.TargetId, out var contacts))
            {
                contacts = new List<ContactPictureEntry>();
                contactsByTarget[entry.TargetId] = contacts;
            }

            contacts.Add(entry);
        }

        return contactsByTarget;
    }

    private static Dictionary<string, ContactPictureEntry> BuildRepresentativeContactsByTarget(
        IReadOnlyDictionary<string, List<ContactPictureEntry>> contactsByTarget)
    {
        var byTarget = new Dictionary<string, ContactPictureEntry>(contactsByTarget.Count, StringComparer.Ordinal);
        foreach (var (targetId, contacts) in contactsByTarget)
        {
            byTarget[targetId] = contacts[0];
        }

        return byTarget;
    }

    private static List<ContactChangeRecord> FanOutBdaContactChanges(
        IReadOnlyList<ContactChangeRecord> perTargetChanges,
        IReadOnlyDictionary<string, List<ContactPictureEntry>> contactsByTarget)
    {
        if (perTargetChanges.Count == 0)
        {
            return new List<ContactChangeRecord>();
        }

        var expanded = new List<ContactChangeRecord>(perTargetChanges.Count);
        for (var i = 0; i < perTargetChanges.Count; i++)
        {
            var change = perTargetChanges[i];
            if (!contactsByTarget.TryGetValue(change.TargetId, out var contacts) || contacts.Count <= 1)
            {
                expanded.Add(change);
                continue;
            }

            for (var j = 0; j < contacts.Count; j++)
            {
                var contact = contacts[j];
                expanded.Add(new ContactChangeRecord(
                    change.SequenceId,
                    change.SimTime,
                    change.SimTick,
                    contact.ObserverId,
                    contact.ContactId,
                    contact.TargetId,
                    change.PreviousState,
                    change.NewState));
            }
        }

        return expanded;
    }

    private static void AppendJoined(System.Text.StringBuilder builder, IReadOnlyList<ulong> values)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (i > 0)
            {
                builder.Append('+');
            }

            builder.Append(values[i]);
        }
    }

    private static void AppendJoined(System.Text.StringBuilder builder, IReadOnlyList<string> values)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (i > 0)
            {
                builder.Append('+');
            }

            builder.Append(values[i]);
        }
    }

    private static void ApplyChange(
        Dictionary<string, TrackAcc> tracks,
        ContactChangeRecord change,
        IKillChainFireControlSource? fireControl,
        List<KillChainContactTransition> transitions)
    {
        if (!tracks.TryGetValue(change.ContactId, out var track))
        {
            track = new TrackAcc(
                change.ContactId,
                change.TargetId,
                change.ObserverId,
                change.SimTick,
                change.SimTime);
            tracks[change.ContactId] = track;
        }

        track.ObserverId = change.ObserverId;
        track.TargetId = change.TargetId;
        track.Lifecycle = change.NewState;
        track.LastSimTick = change.SimTick;
        track.LastSimTime = change.SimTime;
        track.CorrelationSequenceId = change.SequenceId;
        track.RecordSequence(change.SequenceId);

        if (IsLost(change.NewState))
        {
            track.Loss = KillChainLossKind.Lost;
        }
        else if (IsDegradedL2(change.NewState))
        {
            track.PromoteLoss(KillChainLossKind.DegradedL2);
        }
        else if (IsDegradedL1(change.NewState))
        {
            track.PromoteLoss(KillChainLossKind.DegradedL1);
        }
        else if (IsDetection(change.NewState) || IsLocalized(change.NewState))
        {
            if (track.Loss is KillChainLossKind.Lost or KillChainLossKind.Stale)
            {
                track.Loss = KillChainLossKind.None;
                track.ResetPublishedPromotions();
            }
        }

        RefreshCapabilities(track, change.SimTick, fireControl);
        Publish(track, change.SimTick, change.SimTime, transitions);
    }

    private static void ApplyFreshness(
        TrackAcc track,
        ulong currentSimTick,
        IKillChainFireControlSource? fireControl,
        int staleTicks,
        int dropTicks,
        List<KillChainContactTransition> transitions)
    {
        if (track.Loss != KillChainLossKind.Lost)
        {
            var age = currentSimTick >= track.LastSimTick
                ? currentSimTick - track.LastSimTick
                : 0UL;
            if (age > (ulong)dropTicks)
            {
                track.PromoteLoss(KillChainLossKind.Lost);
            }
            else if (age > (ulong)staleTicks)
            {
                track.PromoteLoss(KillChainLossKind.Stale);
            }
        }

        RefreshCapabilities(track, currentSimTick, fireControl);
        var simTime = currentSimTick == track.LastSimTick
            ? track.LastSimTime
            : currentSimTick;
        Publish(track, currentSimTick, simTime, transitions);
    }

    private static void RefreshCapabilities(
        TrackAcc track,
        ulong evaluationTick,
        IKillChainFireControlSource? fireControl)
    {
        var hasFc = fireControl is not null
            && fireControl.HasFireControlTrack(track.ContactId, track.TargetId);
        var lost = track.Loss == KillChainLossKind.Lost;
        var stale = track.Loss == KillChainLossKind.Stale;
        var bdaDegraded = track.Loss is KillChainLossKind.DegradedL1 or KillChainLossKind.DegradedL2;

        if (IsDetection(track.Lifecycle) || IsLocalized(track.Lifecycle) || IsLost(track.Lifecycle))
        {
            track.DetectionCaptured = true;
        }

        track.LocationSufficient = !lost
            && (hasFc || IsLocalized(track.Lifecycle) || track.LocationSufficient);

        var custodyEvidence = IsLocalized(track.Lifecycle)
            || track.SourceSequenceIds.Count > 1
            || track.LastSimTick < evaluationTick;

        track.TrackContinuous = !lost
            && !stale
            && track.LocationSufficient
            && custodyEvidence;

        track.Targetable = track.TrackContinuous
            && hasFc
            && track.Loss == KillChainLossKind.None;

        if (lost)
        {
            track.TrackContinuous = false;
            track.Targetable = false;
        }

        track.Phase = ResolvePhase(track.Targetable, track.TrackContinuous, track.LocationSufficient, track.DetectionCaptured);

        // BDA degradation is not a kill-chain promotion; drop Target while custody remains.
        if (bdaDegraded && track.Phase == KillChainPhase.Target)
        {
            track.Phase = KillChainPhase.Track;
        }
    }

    private static KillChainPhase ResolvePhase(
        bool targetable,
        bool trackContinuous,
        bool locationSufficient,
        bool detectionCaptured)
    {
        if (targetable)
        {
            return KillChainPhase.Target;
        }

        if (trackContinuous)
        {
            return KillChainPhase.Track;
        }

        if (locationSufficient)
        {
            return KillChainPhase.Fix;
        }

        return detectionCaptured ? KillChainPhase.Find : KillChainPhase.None;
    }

    private static void Publish(
        TrackAcc track,
        ulong simTick,
        double simTime,
        List<KillChainContactTransition> transitions)
    {
        if (track.DetectionCaptured)
        {
            TryEmit(track, KillChainTransitionKind.Find, KillChainPhase.Find, KillChainLossKind.None, simTick, simTime, transitions);
        }

        if (track.LocationSufficient)
        {
            TryEmit(track, KillChainTransitionKind.Fix, KillChainPhase.Fix, KillChainLossKind.None, simTick, simTime, transitions);
        }

        if (track.TrackContinuous)
        {
            TryEmit(track, KillChainTransitionKind.Track, KillChainPhase.Track, KillChainLossKind.None, simTick, simTime, transitions);
        }

        if (track.Targetable)
        {
            TryEmit(track, KillChainTransitionKind.Target, KillChainPhase.Target, KillChainLossKind.None, simTick, simTime, transitions);
        }

        if (track.Loss == KillChainLossKind.Lost)
        {
            TryEmit(track, KillChainTransitionKind.Lost, track.Phase, track.Loss, simTick, simTime, transitions);
        }
        else if (track.Loss != KillChainLossKind.None)
        {
            TryEmit(track, KillChainTransitionKind.Degraded, track.Phase, track.Loss, simTick, simTime, transitions);
        }
    }

    private static void TryEmit(
        TrackAcc track,
        KillChainTransitionKind kind,
        KillChainPhase newPhase,
        KillChainLossKind loss,
        ulong simTick,
        double simTime,
        List<KillChainContactTransition> transitions)
    {
        if (!track.Emitted.Add(kind))
        {
            return;
        }

        var previous = track.LastEmittedPhase;
        transitions.Add(new KillChainContactTransition(
            kind,
            track.ContactId,
            track.TargetId,
            track.ObserverId,
            previous,
            newPhase,
            loss,
            simTick,
            simTime,
            track.CorrelationSequenceId,
            track.BuildSourceRefs()));
        track.LastEmittedPhase = newPhase;
    }

    private static bool IsDetection(string state) =>
        string.Equals(state, "Detected", StringComparison.Ordinal)
        || IsLocalized(state);

    private static bool IsLocalized(string state) =>
        string.Equals(state, "Classified", StringComparison.Ordinal)
        || string.Equals(state, "Identified", StringComparison.Ordinal)
        || IsDegradedL1(state)
        || IsDegradedL2(state);

    private static bool IsLost(string state) =>
        string.Equals(state, "Lost", StringComparison.Ordinal)
        || string.Equals(state, BdaContactDamageStates.Lost, StringComparison.Ordinal);

    private static bool IsDegradedL1(string state) =>
        string.Equals(state, BdaContactDamageStates.DegradedL1, StringComparison.Ordinal);

    private static bool IsDegradedL2(string state) =>
        string.Equals(state, BdaContactDamageStates.DegradedL2, StringComparison.Ordinal);

    private sealed class TrackAcc
    {
        public TrackAcc(string contactId, string targetId, string observerId, ulong firstTick, double firstTime)
        {
            ContactId = contactId;
            TargetId = targetId;
            ObserverId = observerId;
            FirstSimTick = firstTick;
            FirstSimTime = firstTime;
            LastSimTick = firstTick;
            LastSimTime = firstTime;
        }

        public string ContactId { get; }

        public string TargetId { get; set; }

        public string ObserverId { get; set; }

        public string Lifecycle { get; set; } = "Unknown";

        public KillChainPhase Phase { get; set; }

        public KillChainLossKind Loss { get; set; }

        public bool DetectionCaptured { get; set; }

        public bool LocationSufficient { get; set; }

        public bool TrackContinuous { get; set; }

        public bool Targetable { get; set; }

        public ulong FirstSimTick { get; }

        public double FirstSimTime { get; }

        public ulong LastSimTick { get; set; }

        public double LastSimTime { get; set; }

        public ulong CorrelationSequenceId { get; set; }

        public List<ulong> SourceSequenceIds { get; } = new();

        public HashSet<KillChainTransitionKind> Emitted { get; } = new();

        public KillChainPhase LastEmittedPhase { get; set; }

        public void RecordSequence(ulong sequenceId)
        {
            if (SourceSequenceIds.Count > 0 && SourceSequenceIds[^1] == sequenceId)
            {
                return;
            }

            SourceSequenceIds.Add(sequenceId);
        }

        public void PromoteLoss(KillChainLossKind candidate)
        {
            if ((int)candidate > (int)Loss)
            {
                if (Loss != KillChainLossKind.None)
                {
                    Emitted.Remove(KillChainTransitionKind.Degraded);
                }

                Loss = candidate;
            }
        }

        public void ResetPublishedPromotions()
        {
            Emitted.Remove(KillChainTransitionKind.Find);
            Emitted.Remove(KillChainTransitionKind.Fix);
            Emitted.Remove(KillChainTransitionKind.Track);
            Emitted.Remove(KillChainTransitionKind.Target);
            Emitted.Remove(KillChainTransitionKind.Degraded);
            Emitted.Remove(KillChainTransitionKind.Lost);
            LastEmittedPhase = KillChainPhase.None;
        }

        public string[] BuildSourceRefs()
        {
            var refs = new[]
            {
                $"contact:{ContactId}",
                $"observer:{ObserverId}",
                $"target:{TargetId}",
                $"seq:{CorrelationSequenceId}",
            };
            Array.Sort(refs, StringComparer.Ordinal);
            return refs;
        }

        public KillChainContactState ToState() =>
            new(
                ContactId,
                TargetId,
                ObserverId,
                Phase,
                Loss,
                DetectionCaptured,
                LocationSufficient,
                TrackContinuous,
                Targetable,
                FirstSimTick,
                FirstSimTime,
                LastSimTick,
                LastSimTime,
                CorrelationSequenceId,
                SourceSequenceIds.ToArray(),
                BuildSourceRefs());
    }
}
