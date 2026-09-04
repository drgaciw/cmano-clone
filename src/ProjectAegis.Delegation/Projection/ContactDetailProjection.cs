namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// CMD-29: builds contact-detail presentation from the sensor contact picture (and optional BDA).
/// Honest interim labels — no fabricated platform classes (e.g. no fake SA-3).
/// </summary>
public static class ContactDetailProjection
{
    public static ContactDetailEntry? Project(
        ContactPictureEntry? contact,
        ulong currentSimTick,
        string? bdaLifecycleOverride = null,
        string? sourceKind = null,
        bool outOfComms = false,
        string? lastKnownState = null)
    {
        if (contact is null)
        {
            return null;
        }

        var lifecycle = string.IsNullOrEmpty(bdaLifecycleOverride)
            ? contact.LifecycleState
            : bdaLifecycleOverride!;

        var ageTicks = currentSimTick >= contact.LastSimTick
            ? currentSimTick - contact.LastSimTick
            : 0UL;

        var known = string.IsNullOrEmpty(lastKnownState) ? lifecycle : lastKnownState!;
        var source = string.IsNullOrWhiteSpace(sourceKind) ? "observer" : sourceKind.Trim();

        return new ContactDetailEntry(
            contact.ContactId,
            contact.TargetId,
            FormatClassification(contact.TargetId, lifecycle),
            FormatConfidence(lifecycle),
            $"Detected by: {contact.ObserverId}",
            FormatWra(lifecycle),
            FormatBda(lifecycle),
            $"{ageTicks} ticks stale",
            lifecycle,
            $"SOURCE: {source}",
            outOfComms ? "COMMS: UNKNOWN (out-of-comms)" : "COMMS: up",
            $"LAST KNOWN: {known}",
            $"EXPLAIN: engage/{contact.ContactId}");
    }

    public static ContactDetailEntry? Project(
        string contactId,
        IReadOnlyList<ContactPictureEntry> contacts,
        ulong currentSimTick,
        string? bdaLifecycleOverride = null,
        string? sourceKind = null,
        bool outOfComms = false,
        string? lastKnownState = null)
    {
        if (string.IsNullOrEmpty(contactId) || contacts is null)
        {
            return null;
        }

        var match = contacts.FirstOrDefault(c =>
            string.Equals(c.ContactId, contactId, StringComparison.Ordinal));
        return Project(match, currentSimTick, bdaLifecycleOverride, sourceKind, outOfComms, lastKnownState);
    }

    private static string FormatClassification(string targetId, string lifecycle) =>
        // Interim classification: lifecycle + track id — no overconfident platform name.
        $"CLASS: {lifecycle} · track {targetId}";

    private static string FormatConfidence(string lifecycle) =>
        lifecycle switch
        {
            "Identified" => "CONF: high (identified)",
            "Classified" => "CONF: medium (classified)",
            "Detected" => "CONF: low (detected)",
            BdaContactDamageStates.DegradedL1 => "CONF: assessed (BDA L1)",
            BdaContactDamageStates.DegradedL2 => "CONF: assessed (BDA L2)",
            BdaContactDamageStates.Lost => "CONF: lost track",
            _ => "CONF: unknown",
        };

    private static string FormatWra(string lifecycle) =>
        // WRA is evaluated on the engage path; panel surfaces doctrine rules and salvo budget.
        lifecycle switch
        {
            "Identified"
                or BdaContactDamageStates.DegradedL1
                or BdaContactDamageStates.DegradedL2
                => "WRA: evaluate before fire · Salvo budget OK",
            "Classified" => "WRA: class pending evaluation",
            "Detected" => "WRA: insufficient ID",
            BdaContactDamageStates.Lost => "WRA: track lost",
            _ => "WRA: —",
        };

    private static string FormatBda(string lifecycle)
    {
        // BDA as belief: only when lifecycle is a BDA damage state (or Lost via BDA).
        if (BdaContactDamageStates.Rank(lifecycle) > 0)
        {
            var desc = lifecycle switch
            {
                BdaContactDamageStates.DegradedL1 => "L1 moderate damage",
                BdaContactDamageStates.DegradedL2 => "L2 heavy damage",
                BdaContactDamageStates.Lost => "target lost / destroyed",
                _ => "assessed",
            };
            return $"BDA: {lifecycle} (assessment · {desc})";
        }

        return "BDA: —";
    }
}
