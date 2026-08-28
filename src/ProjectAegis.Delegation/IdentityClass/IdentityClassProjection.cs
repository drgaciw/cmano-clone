namespace ProjectAegis.Delegation.IdentityClass;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Scenario;

/// <summary>
/// DRG-225: folds contact picture and provenance facts into a replay-stable unknown-vs-known
/// identity ledger. Advisory only — never enqueues fire orders or authorization.
/// </summary>
public static class IdentityClassProjection
{
    public static IdentityClassSnapshot Project(
        DecisionLog? log,
        ulong currentSimTick,
        ICatalogReader? catalog = null,
        ScenarioCommsDisplaySettings? commsDisplay = null,
        int staleThresholdTicks = ContactProvenanceProjection.DefaultStaleThresholdTicks,
        IReadOnlyList<ScenarioOrbatUnitDto>? orbatUnits = null)
    {
        if (log is null)
        {
            return IdentityClassSnapshot.Empty;
        }

        var contacts = ContactPictureProjection.Project(log);
        if (contacts.Count == 0)
        {
            return IdentityClassSnapshot.Empty;
        }

        var provenance = ContactProvenanceProjection.Project(
            log,
            currentSimTick,
            catalog,
            commsDisplay,
            staleThresholdTicks,
            orbatUnits);
        var provenanceByContact = provenance.Contacts.ToDictionary(c => c.ContactId, StringComparer.Ordinal);

        var rows = new IdentityClassRow[contacts.Count];
        for (var i = 0; i < contacts.Count; i++)
        {
            var contact = contacts[i];
            var prov = provenanceByContact.GetValueOrDefault(contact.ContactId);
            rows[i] = BuildRow(contact, prov);
        }

        Array.Sort(rows, CompareRows);
        return new IdentityClassSnapshot(rows);
    }

    private static IdentityClassRow BuildRow(
        ContactPictureEntry contact,
        ContactProvenanceState? provenance)
    {
        var classification = ResolveClassification(contact, provenance);
        var reasonCode = ResolveReasonCode(contact, provenance, classification);
        var confidence = ResolveConfidenceBand(contact, provenance, classification);
        return new IdentityClassRow(
            contact.ContactId,
            classification,
            reasonCode,
            confidence,
            contact.LastSimTick);
    }

    private static IdentityClassification ResolveClassification(
        ContactPictureEntry contact,
        ContactProvenanceState? provenance)
    {
        if (provenance?.OutOfCommsUnknown == true)
        {
            return IdentityClassification.Unknown;
        }

        if (provenance?.QualityState.HasFlag(ContactProvenanceQualityState.CatalogMiss) == true
            && IsUnknownLifecycle(contact.LifecycleState))
        {
            return IdentityClassification.Unknown;
        }

        if (string.Equals(contact.LifecycleState, "Unknown", StringComparison.Ordinal))
        {
            return IdentityClassification.Unknown;
        }

        if (string.Equals(contact.LifecycleState, "Detected", StringComparison.Ordinal))
        {
            return IdentityClassification.Tentative;
        }

        if (IsClassifiedLifecycle(contact.LifecycleState))
        {
            return IdentityClassification.Classified;
        }

        return IdentityClassification.Unknown;
    }

    private static string ResolveReasonCode(
        ContactPictureEntry contact,
        ContactProvenanceState? provenance,
        IdentityClassification classification)
    {
        if (provenance?.OutOfCommsUnknown == true)
        {
            return IdentityClassReasonCodes.CommsGap;
        }

        if (provenance?.QualityState.HasFlag(ContactProvenanceQualityState.CatalogMiss) == true
            && classification == IdentityClassification.Unknown)
        {
            return IdentityClassReasonCodes.CatalogMiss;
        }

        if (provenance?.QualityState.HasFlag(ContactProvenanceQualityState.Stale) == true
            && classification == IdentityClassification.Tentative)
        {
            return IdentityClassReasonCodes.StaleTrack;
        }

        if (string.Equals(contact.LifecycleState, "Unknown", StringComparison.Ordinal))
        {
            return IdentityClassReasonCodes.LifecycleUnknown;
        }

        if (string.Equals(contact.LifecycleState, "Detected", StringComparison.Ordinal))
        {
            return IdentityClassReasonCodes.LifecycleDetected;
        }

        if (string.Equals(contact.LifecycleState, "Identified", StringComparison.Ordinal))
        {
            return IdentityClassReasonCodes.LifecycleIdentified;
        }

        if (IsClassifiedLifecycle(contact.LifecycleState))
        {
            return IdentityClassReasonCodes.LifecycleClassified;
        }

        return IdentityClassReasonCodes.LifecycleUnknown;
    }

    private static IdentityConfidenceBand ResolveConfidenceBand(
        ContactPictureEntry contact,
        ContactProvenanceState? provenance,
        IdentityClassification classification)
    {
        if (classification == IdentityClassification.Unknown)
        {
            return IdentityConfidenceBand.Unknown;
        }

        if (provenance is not null)
        {
            return provenance.Confidence switch
            {
                ContactProvenanceConfidence.High => IdentityConfidenceBand.High,
                ContactProvenanceConfidence.Medium => IdentityConfidenceBand.Medium,
                ContactProvenanceConfidence.Low => IdentityConfidenceBand.Low,
                _ => IdentityConfidenceBand.Unknown,
            };
        }

        if (string.Equals(contact.LifecycleState, "Identified", StringComparison.Ordinal))
        {
            return IdentityConfidenceBand.High;
        }

        if (IsClassifiedLifecycle(contact.LifecycleState))
        {
            return IdentityConfidenceBand.Medium;
        }

        if (string.Equals(contact.LifecycleState, "Detected", StringComparison.Ordinal))
        {
            return IdentityConfidenceBand.Low;
        }

        return IdentityConfidenceBand.Unknown;
    }

    private static bool IsUnknownLifecycle(string lifecycleState) =>
        string.Equals(lifecycleState, "Unknown", StringComparison.Ordinal);

    private static bool IsClassifiedLifecycle(string lifecycleState) =>
        string.Equals(lifecycleState, "Classified", StringComparison.Ordinal)
        || string.Equals(lifecycleState, "Identified", StringComparison.Ordinal)
        || string.Equals(lifecycleState, BdaContactDamageStates.DegradedL1, StringComparison.Ordinal)
        || string.Equals(lifecycleState, BdaContactDamageStates.DegradedL2, StringComparison.Ordinal);

    private static int CompareRows(IdentityClassRow? left, IdentityClassRow? right)
    {
        if (left is null && right is null)
        {
            return 0;
        }

        if (left is null)
        {
            return -1;
        }

        if (right is null)
        {
            return 1;
        }

        return string.Compare(left.ContactId, right.ContactId, StringComparison.Ordinal);
    }
}
