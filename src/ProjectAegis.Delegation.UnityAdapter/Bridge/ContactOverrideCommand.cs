namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using Decision;
using Projection;

/// <summary>Outcome of a player contact operator override command (DRG-272 / C2-22).</summary>
public readonly record struct ContactOverrideResult(bool Accepted, string? ReasonCode, string? Message);

/// <summary>
/// ADR-010 command facade for contact operator overrides (reclassify / drop).
/// Appends <see cref="ContactOverrideRecord"/> to <see cref="DecisionLog"/> without mutating
/// sensor <see cref="ContactChangeRecord"/> truth, catalog SQLite DBs, or <c>SimulationSession</c>.
/// Must not reference <c>UnityAdapter.Presentation</c> or <c>UnityAdapter.CommandReview</c>.
/// </summary>
public static class ContactOverrideCommand
{
    public const string ReasonReplayAttached = C2PlayerCommandBridge.ReasonReplayAttached;
    public const string ReasonMissingContactId = "MISSING_CONTACT_ID";
    public const string ReasonUnknownContact = "UNKNOWN_CONTACT";
    public const string ReasonInvalidAffiliation = "INVALID_AFFILIATION";
    public const string ReasonNoChange = "NO_CHANGE";

    public static ContactOverrideResult TryReclassify(
        DelegationBridge? bridge,
        string? contactId,
        string? affiliation,
        double simTime)
    {
        if (bridge is null)
        {
            return new ContactOverrideResult(false, ReasonUnknownContact, "Bridge unavailable.");
        }

        if (bridge.AttachReplayViewer)
        {
            return new ContactOverrideResult(false, ReasonReplayAttached,
                "Replay viewer attached; contact overrides are disabled.");
        }

        if (string.IsNullOrWhiteSpace(contactId))
        {
            return new ContactOverrideResult(false, ReasonMissingContactId, "No contact selected.");
        }

        var canonical = ResolveCanonicalAffiliation(affiliation);
        if (canonical is null)
        {
            return new ContactOverrideResult(false, ReasonInvalidAffiliation,
                $"Unsupported affiliation '{affiliation}'.");
        }

        var log = bridge.Orchestrator.DecisionLog;
        if (!HasActiveContact(log, contactId))
        {
            return new ContactOverrideResult(false, ReasonUnknownContact,
                $"Contact '{contactId}' is not active.");
        }

        var current = ContactOverrideProjection.Project(log).Find(contactId);
        if (current is { IsDropped: false } &&
            string.Equals(current.AffiliationOverride, canonical, StringComparison.Ordinal))
        {
            return new ContactOverrideResult(false, ReasonNoChange,
                $"Contact {contactId} is already classified as {canonical}.");
        }

        var previous = current is null
            ? "Hostile"
            : current.IsDropped
                ? "Dropped"
                : current.AffiliationOverride ?? "Hostile";

        var simTick = (ulong)Math.Max(0, (long)simTime);
        log.AppendContactOverride(new ContactOverrideRecord(
            SequenceId: 0,
            SimTime: simTime,
            SimTick: simTick,
            ContactId: contactId,
            Action: ContactOverrideAction.Reclassify,
            PreviousValue: previous,
            NewValue: canonical));

        return new ContactOverrideResult(true, null,
            $"Contact {contactId} reclassified to {canonical}.");
    }

    public static ContactOverrideResult TryDrop(
        DelegationBridge? bridge,
        string? contactId,
        double simTime)
    {
        if (bridge is null)
        {
            return new ContactOverrideResult(false, ReasonUnknownContact, "Bridge unavailable.");
        }

        if (bridge.AttachReplayViewer)
        {
            return new ContactOverrideResult(false, ReasonReplayAttached,
                "Replay viewer attached; contact overrides are disabled.");
        }

        if (string.IsNullOrWhiteSpace(contactId))
        {
            return new ContactOverrideResult(false, ReasonMissingContactId, "No contact selected.");
        }

        var log = bridge.Orchestrator.DecisionLog;
        if (!HasActiveContact(log, contactId))
        {
            return new ContactOverrideResult(false, ReasonUnknownContact,
                $"Contact '{contactId}' is not active.");
        }

        var current = ContactOverrideProjection.Project(log).Find(contactId);
        if (current?.IsDropped == true)
        {
            return new ContactOverrideResult(false, ReasonNoChange,
                $"Contact {contactId} is already dropped.");
        }

        var previous = current?.AffiliationOverride ?? "Hostile";
        var simTick = (ulong)Math.Max(0, (long)simTime);
        log.AppendContactOverride(new ContactOverrideRecord(
            SequenceId: 0,
            SimTime: simTime,
            SimTick: simTick,
            ContactId: contactId,
            Action: ContactOverrideAction.Drop,
            PreviousValue: previous,
            NewValue: "Dropped"));

        return new ContactOverrideResult(true, null,
            $"Contact {contactId} dropped by operator.");
    }

    private static bool HasActiveContact(DecisionLog log, string contactId)
    {
        var contacts = ContactPictureProjection.Project(log);
        for (var i = 0; i < contacts.Count; i++)
        {
            if (string.Equals(contacts[i].ContactId, contactId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string? ResolveCanonicalAffiliation(string? affiliation)
    {
        if (string.IsNullOrWhiteSpace(affiliation))
        {
            return null;
        }

        var trimmed = affiliation.Trim();
        foreach (var supported in MilitarySymbology.SupportedAffiliations)
        {
            if (string.Equals(supported, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return supported;
            }
        }

        return null;
    }
}
