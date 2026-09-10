namespace ProjectAegis.Delegation.UnityAdapter.CommandReview;

using Projection;
using ResourceRank;
using SensorToShooter;
using Skills;
using Bridge;

/// <summary>Builds tick-bound advisory facts without advancing or mutating simulation state.</summary>
public static class AdviceBridge
{
    /// <summary>Builds from standard runtime facts plus an optional snapshot advisory capability.</summary>
    public static AdviceFrame Build(DelegationBridge bridge, ISimWorldSnapshot snapshot, SliceAContactFrame contacts)
        => Build(bridge, snapshot, contacts, contacts?.Contacts.FirstOrDefault()?.ContactId);

    /// <summary>Builds standard runtime advice for the explicitly selected current contact.</summary>
    public static AdviceFrame Build(DelegationBridge bridge, ISimWorldSnapshot snapshot, SliceAContactFrame contacts, string? selectedContactId)
    {
        if (contacts is null) throw new ArgumentNullException(nameof(contacts));
        var contactId = selectedContactId ?? string.Empty;
        AdviceEvidenceSource? supplied = null;
        if (snapshot is IAdviceEvidenceSource source && contactId.Length > 0
            && source.TryGetAdviceEvidence(contactId, out var candidate)) supplied = candidate;
        supplied ??= AdviceRuntimeEvidenceBridge.Build(bridge, snapshot, contacts, contactId);
        return BuildCore(bridge, snapshot, contacts, supplied, contactId);
    }

    /// <summary>Builds from supplied current projections; mismatched or absent facts fail closed.</summary>
    public static AdviceFrame Build(DelegationBridge bridge, ISimWorldSnapshot snapshot, SliceAContactFrame contacts, AdviceEvidenceSource? supplied)
        => BuildCore(bridge, snapshot, contacts, supplied, supplied?.ContactId ?? contacts?.Contacts.FirstOrDefault()?.ContactId ?? string.Empty);

    private static AdviceFrame BuildCore(DelegationBridge bridge, ISimWorldSnapshot snapshot, SliceAContactFrame contacts, AdviceEvidenceSource? supplied, string contactId)
    {
        if (bridge is null) throw new ArgumentNullException(nameof(bridge));
        if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));
        if (contacts is null) throw new ArgumentNullException(nameof(contacts));
        var simTime = snapshot.SimTime;
        var tick = double.IsFinite(simTime) && simTime > 0 ? (ulong)simTime : 0UL;
        if (!double.IsFinite(simTime) || simTime < 0 || contacts.SimTime is null || !double.IsFinite(contacts.SimTime.Value)
            || contacts.SimTime.Value < 0 || contacts.SimTime.Value != simTime)
            return AdviceFrame.Unavailable(tick, snapshot.SimTime, AdviceAvailability.Stale, "refresh the contact frame before review", contactId);
        if (supplied is { ModelAvailable: false })
            return AdviceFrame.Unavailable(tick, snapshot.SimTime, AdviceAvailability.ModelUnavailable, "use current contact facts for manual review", contactId);

        var contact = contacts.Contacts.FirstOrDefault(c => string.Equals(c.ContactId, contactId, StringComparison.Ordinal));
        if (contact is null)
            return AdviceFrame.Unavailable(tick, snapshot.SimTime, AdviceAvailability.EvidenceUnavailable, "select a current contact and refresh evidence", contactId);
        if (!double.IsFinite(contact.LastSimTime) || contact.LastSimTime < 0 || contact.LastSimTime > simTime)
            return AdviceFrame.Unavailable(tick, snapshot.SimTime, AdviceAvailability.Stale, "refresh future-dated contact evidence", contactId);
        if (supplied is not null && (!string.Equals(supplied.ContactId, contactId, StringComparison.Ordinal)
            || !double.IsFinite(supplied.SimTime) || supplied.SimTime < 0 || supplied.SimTime != simTime))
            return AdviceFrame.Unavailable(tick, snapshot.SimTime, AdviceAvailability.Stale, "refresh advisory evidence before review", contactId);
        if (supplied?.ThreatAssessment is { } threat &&
            (!string.Equals(threat.ContactId, contactId, StringComparison.Ordinal) || !string.Equals(threat.TargetId, contact.TargetId, StringComparison.Ordinal)))
            return AdviceFrame.Unavailable(tick, snapshot.SimTime, AdviceAvailability.EvidenceUnavailable, "discard mismatched threat evidence and refresh", contactId);
        if (supplied?.ResourceRanking is { } ranking &&
            (!string.Equals(ranking.ContactId, contactId, StringComparison.Ordinal) || !string.Equals(ranking.TargetId, contact.TargetId, StringComparison.Ordinal)
             || ranking.RankedCandidates.Any(c => !string.Equals(c.ContactId, contactId, StringComparison.Ordinal) || !string.Equals(c.TargetId, contact.TargetId, StringComparison.Ordinal))))
            return AdviceFrame.Unavailable(tick, snapshot.SimTime, AdviceAvailability.EvidenceUnavailable, "discard mismatched resource evidence and refresh", contactId);

        var evidence = new List<EvidencePointer>
        {
            new(EvidenceKind.Contact, contactId, null, nameof(ContactPictureEntry), null),
            new(EvidenceKind.Snapshot, "world", null, nameof(ISimWorldSnapshot), nameof(ISimWorldSnapshot.SimTime)),
        };
        var provenance = contacts.Provenance.Contacts.FirstOrDefault(p => string.Equals(p.ContactId, contactId, StringComparison.Ordinal));
        var chain = contacts.Chains.Chains.FirstOrDefault(c => string.Equals(c.ContactId, contactId, StringComparison.Ordinal));
        var authority = contacts.Authorities.TryGetValue(contactId, out var projectedAuthority) ? projectedAuthority : null;
        var assumptions = new List<string>();
        var policy = new List<string>();
        var alternatives = new List<string>();
        var commitments = new List<string>();
        double? confidence = supplied?.ThreatAssessment?.Confidence;
        if (provenance is { Freshness: ContactProvenanceFreshness.Stale } || provenance?.OutOfCommsUnknown == true ||
            (provenance is not null && (provenance.QualityState & (ContactProvenanceQualityState.Stale | ContactProvenanceQualityState.SilentComms)) != 0))
            return AdviceFrame.Unavailable(tick, snapshot.SimTime, AdviceAvailability.Stale, "refresh or restore the reporting link before relying on advice", contactId);
        assumptions.AddRange(supplied?.ThreatAssessment?.Assumptions ?? ["No weapon-specific assessment supplied; recommendation is limited to contact and chain facts."]);
        if (chain is null) assumptions.Add("Sensor-to-shooter chain unavailable.");
        else
        {
            evidence.Add(new(EvidenceKind.Projection, contactId, null, nameof(SensorToShooterSnapshot), "Chains"));
            if (!chain.IsComplete) alternatives.Add($"restore-chain:{chain.PrimaryCauseLabel}");
        }
        if (authority is null) policy.Add("authority:unknown:no implicit permission");
        else policy.Add($"authority:{authority.Targeting.Disposition}:{authority.Targeting.ReasonCode ?? "none"}");

        foreach (var candidate in supplied?.ResourceRanking?.RankedCandidates ?? [])
        {
            var line = $"{candidate.ShooterUnitId}/{candidate.WeaponId}: {candidate.ReasonPlain}";
            var scores = candidate.Scores;
            var scored = $"{line} | scores effect={scores.ExpectedEffect:0.###}, time={scores.Time:0.###}, availability={scores.Availability:0.###}, commitment={scores.Commitment:0.###}, conservation={scores.Conservation:0.###}, total={scores.Total:0.###}";
            alternatives.Add($"{candidate.Disposition.ToString().ToLowerInvariant()}:{scored}");
            if (candidate.Disposition == ResourceRankDisposition.Excluded) commitments.Add(line);
        }
        if (supplied?.ThreatAssessment is not null)
        {
            evidence.Add(new(EvidenceKind.Projection, contactId, null, "WeaponRecommendation", null));
            policy.Add($"weapon-policy:{supplied.ThreatAssessment.PolicyConstraints.RoeLevel}:{supplied.ThreatAssessment.WithheldReasonCode ?? "none"}");
            var range = supplied.ThreatAssessment.Range;
            policy.Add($"range:{range.RangeMeters:0.###}m:envelope={range.EnvelopeMinMeters:0.###}-{range.EnvelopeMaxMeters:0.###}m:{range.DlzState}:in-envelope={range.InEnvelope}");
        }
        if (supplied?.ResourceRanking is not null) evidence.Add(new(EvidenceKind.Projection, contactId, null, nameof(ResourceRankSnapshot), null));

        return new AdviceFrame(contactId, tick, snapshot.SimTime, AdviceAvailability.Available, confidence,
            provenance?.Confidence.ToString().ToUpperInvariant() ?? "UNKNOWN", evidence.AsReadOnly(), assumptions.AsReadOnly(),
            supplied?.ThreatAssessment?.StatusLine ?? (chain?.IsComplete == true ? "Current technical chain is complete; authority remains separate." : "Review current contact and chain limitations."),
            ["advisory-only:no-authority-or-order", "execution-must-revalidate-current-facts"], policy.AsReadOnly(), alternatives.AsReadOnly(), commitments.AsReadOnly(),
            supplied?.MissionPackageFacts ?? [], authority, "manual review of cited current facts", false, false);
    }
}
