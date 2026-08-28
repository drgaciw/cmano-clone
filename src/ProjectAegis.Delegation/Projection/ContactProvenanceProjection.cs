namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Sim.Scenario;

/// <summary>
/// DRG-206: folds order-log contact changes and comms state into deterministic provenance rows.
/// Sim-clock only. Does not mutate tick path or UI chrome.
/// </summary>
public static class ContactProvenanceProjection
{
    /// <summary>Matches kill-chain / sensor GDD default stale threshold.</summary>
    public const int DefaultStaleThresholdTicks = 30;

    public static ContactProvenanceSnapshot Project(
        DecisionLog? log,
        ulong currentSimTick,
        ICatalogReader? catalog = null,
        ScenarioCommsDisplaySettings? commsDisplay = null,
        int staleThresholdTicks = DefaultStaleThresholdTicks,
        IReadOnlyList<ScenarioOrbatUnitDto>? orbatUnits = null)
    {
        if (log is null)
        {
            return ContactProvenanceSnapshot.Empty;
        }

        var comms = CommsStateProjection.Project(log);
        var display = commsDisplay ?? ScenarioCommsDisplaySettings.Default;
        var effectiveStale = ComputeEffectiveStaleThreshold(staleThresholdTicks, comms.State, display);
        var unitPlatformMap = BuildUnitPlatformMap(orbatUnits);
        var contacts = ContactPictureProjection.Project(log);
        if (contacts.Count == 0)
        {
            return ContactProvenanceSnapshot.Empty;
        }

        var rows = new ContactProvenanceState[contacts.Count];
        for (var i = 0; i < contacts.Count; i++)
        {
            rows[i] = ProjectContact(
                contacts[i],
                currentSimTick,
                comms.State,
                effectiveStale,
                catalog,
                unitPlatformMap);
        }

        Array.Sort(rows, CompareContacts);
        return new ContactProvenanceSnapshot(rows);
    }

    public static ContactProvenanceSnapshot Project(
        IReadOnlyList<ContactPictureEntry>? contacts,
        ulong currentSimTick,
        CommsState commsState = CommsState.Nominal,
        ICatalogReader? catalog = null,
        ScenarioCommsDisplaySettings? commsDisplay = null,
        int staleThresholdTicks = DefaultStaleThresholdTicks,
        IReadOnlyList<ScenarioOrbatUnitDto>? orbatUnits = null)
    {
        if (contacts is null || contacts.Count == 0)
        {
            return ContactProvenanceSnapshot.Empty;
        }

        var display = commsDisplay ?? ScenarioCommsDisplaySettings.Default;
        var effectiveStale = ComputeEffectiveStaleThreshold(staleThresholdTicks, commsState, display);
        var unitPlatformMap = BuildUnitPlatformMap(orbatUnits);
        var rows = new ContactProvenanceState[contacts.Count];
        for (var i = 0; i < contacts.Count; i++)
        {
            rows[i] = ProjectContact(
                contacts[i],
                currentSimTick,
                commsState,
                effectiveStale,
                catalog,
                unitPlatformMap);
        }

        Array.Sort(rows, CompareContacts);
        return new ContactProvenanceSnapshot(rows);
    }

    internal static int ComputeEffectiveStaleThreshold(
        int staleThresholdTicks,
        CommsState commsState,
        ScenarioCommsDisplaySettings display)
    {
        var baseThreshold = Math.Max(1, staleThresholdTicks);
        var divisor = CommsTrackStaleness.StaleThresholdDivisor(commsState, display);
        return Math.Max(1, baseThreshold / divisor);
    }

    internal static ContactProvenanceState ProjectContact(
        ContactPictureEntry contact,
        ulong currentSimTick,
        CommsState commsState,
        int effectiveStaleThresholdTicks,
        ICatalogReader? catalog,
        IReadOnlyDictionary<string, string>? unitPlatformMap = null)
    {
        var age = currentSimTick >= contact.LastSimTick
            ? currentSimTick - contact.LastSimTick
            : 0UL;
        var isStale = age > (ulong)effectiveStaleThresholdTicks;
        var catalogMiss = IsCatalogMiss(catalog, contact.TargetId, unitPlatformMap);
        var silentComms = commsState is CommsState.Degraded or CommsState.Denied;
        var outOfCommsUnknown = commsState == CommsState.Denied;

        var quality = ContactProvenanceQualityState.None;
        if (catalogMiss)
        {
            quality |= ContactProvenanceQualityState.CatalogMiss;
        }

        if (isStale)
        {
            quality |= ContactProvenanceQualityState.Stale;
        }

        if (silentComms)
        {
            quality |= ContactProvenanceQualityState.SilentComms;
        }

        return new ContactProvenanceState(
            contact.ContactId,
            new ContactProvenanceSource(
                contact.ObserverId,
                contact.TargetId,
                BuildSourceRef(contact.ObserverId, contact.TargetId)),
            ResolveConfidence(contact.LifecycleState),
            isStale ? ContactProvenanceFreshness.Stale : ContactProvenanceFreshness.Fresh,
            age,
            new ContactProvenanceLastKnown(
                contact.LifecycleState,
                contact.TargetId,
                contact.LastSimTick,
                contact.LastSimTime),
            outOfCommsUnknown,
            quality);
    }

    internal static IReadOnlyDictionary<string, string>? BuildUnitPlatformMap(
        IReadOnlyList<ScenarioOrbatUnitDto>? orbatUnits)
    {
        if (orbatUnits is null || orbatUnits.Count == 0)
        {
            return null;
        }

        var map = new Dictionary<string, string>(orbatUnits.Count, StringComparer.Ordinal);
        for (var i = 0; i < orbatUnits.Count; i++)
        {
            var unit = orbatUnits[i];
            if (string.IsNullOrEmpty(unit.Id) || string.IsNullOrEmpty(unit.PlatformId))
            {
                continue;
            }

            map[unit.Id] = unit.PlatformId;
        }

        return map.Count == 0 ? null : map;
    }

    internal static bool IsCatalogMiss(
        ICatalogReader? catalog,
        string targetId,
        IReadOnlyDictionary<string, string>? unitPlatformMap = null)
    {
        if (catalog is null || string.IsNullOrEmpty(targetId))
        {
            return false;
        }

        if (IsCatalogHit(catalog, targetId))
        {
            return false;
        }

        if (unitPlatformMap is not null
            && unitPlatformMap.TryGetValue(targetId, out var platformId)
            && !string.IsNullOrEmpty(platformId)
            && IsCatalogHit(catalog, platformId))
        {
            return false;
        }

        return true;
    }

    private static bool IsCatalogHit(ICatalogReader catalog, string platformOrUnitId) =>
        catalog.TryGetPlatformDomain(platformOrUnitId, out _)
        || catalog.TryGetPlatformPosition(platformOrUnitId, out _, out _);

    private static ContactProvenanceConfidence ResolveConfidence(string lifecycleState)
    {
        if (string.Equals(lifecycleState, "Identified", StringComparison.Ordinal))
        {
            return ContactProvenanceConfidence.High;
        }

        if (string.Equals(lifecycleState, "Classified", StringComparison.Ordinal)
            || string.Equals(lifecycleState, BdaContactDamageStates.DegradedL1, StringComparison.Ordinal)
            || string.Equals(lifecycleState, BdaContactDamageStates.DegradedL2, StringComparison.Ordinal))
        {
            return ContactProvenanceConfidence.Medium;
        }

        if (string.Equals(lifecycleState, "Detected", StringComparison.Ordinal))
        {
            return ContactProvenanceConfidence.Low;
        }

        return ContactProvenanceConfidence.Unknown;
    }

    private static string BuildSourceRef(string observerId, string targetId) =>
        $"observer:{observerId}|target:{targetId}";

    private static int CompareContacts(ContactProvenanceState? left, ContactProvenanceState? right)
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
