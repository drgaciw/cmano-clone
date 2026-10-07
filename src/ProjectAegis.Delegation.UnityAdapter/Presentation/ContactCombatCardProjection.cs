using System.Globalization;
using System.Text;
using ProjectAegis.Delegation.AfterAction;
using ProjectAegis.Delegation.BdaAssess;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.TargetabilityAccept;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// Explicit tokens for the contact combat card (DRG-169).
/// Missing inputs render as <see cref="Unknown"/> — never a guessed classification, posture, or damage state.
/// </summary>
public static class ContactCombatCardTokens
{
    /// <summary>Rendered when a fact was not supplied. Distinct from recorded enum names such as <c>Unknown</c>.</summary>
    public const string Unknown = "UNKNOWN";

    /// <summary>Surface discriminator. Own-unit status (<c>UnitCommsDisplay</c>) is a different type.</summary>
    public const string SurfaceKind = "contact-combat";

    /// <summary>Replay fingerprint when no contact is selected or the snapshot has no cards.</summary>
    public const string EmptyFingerprint = "ccc:empty";
}

/// <summary>
/// Caller-supplied posture for one contact. The card copies <see cref="Label"/> and never invents one.
/// </summary>
public sealed class ContactCombatCardPostureFact
{
    /// <summary>Creates a posture fact for an exact contact id.</summary>
    public ContactCombatCardPostureFact(string contactId, string label)
    {
        ContactId = contactId;
        Label = label;
    }

    /// <summary>Contact the label belongs to. A mismatch is not applied to another contact.</summary>
    public string ContactId { get; }

    /// <summary>Verbatim posture label.</summary>
    public string Label { get; }
}

/// <summary>
/// Immutable combat card for one selected contact (DRG-169).
/// Structurally distinct from own-unit status: provenance, freshness, classification confidence,
/// posture, targetability, engagement status, and BDA only.
/// Presentation-only — ADR-010 §2–3, ADR-007, ADR-001. Does not mutate sim state.
/// </summary>
public sealed class ContactCombatCard
{
    /// <summary>Card with no selected contact. Every fact is <see cref="ContactCombatCardTokens.Unknown"/>.</summary>
    public static ContactCombatCard Empty { get; } = new(
        hasContact: false,
        contactId: ContactCombatCardTokens.Unknown,
        provenance: ContactCombatCardTokens.Unknown,
        freshness: ContactCombatCardTokens.Unknown,
        confidence: ContactCombatCardTokens.Unknown,
        posture: ContactCombatCardTokens.Unknown,
        targetability: ContactCombatCardTokens.Unknown,
        engagementStatus: ContactCombatCardTokens.Unknown,
        bda: ContactCombatCardTokens.Unknown,
        fingerprint: ContactCombatCardTokens.EmptyFingerprint);

    /// <summary>Creates a card from already-resolved fact strings.</summary>
    public ContactCombatCard(
        bool hasContact,
        string contactId,
        string provenance,
        string freshness,
        string confidence,
        string posture,
        string targetability,
        string engagementStatus,
        string bda,
        string fingerprint)
    {
        HasContact = hasContact;
        ContactId = contactId;
        Provenance = provenance;
        Freshness = freshness;
        Confidence = confidence;
        Posture = posture;
        Targetability = targetability;
        EngagementStatus = engagementStatus;
        Bda = bda;
        Fingerprint = string.IsNullOrEmpty(fingerprint)
            ? ContactCombatCardTokens.EmptyFingerprint
            : fingerprint;
    }

    /// <summary>Always <see cref="ContactCombatCardTokens.SurfaceKind"/>.</summary>
    public string SurfaceKind => ContactCombatCardTokens.SurfaceKind;

    /// <summary>True when the caller supplied a contact id. False is the blank selection card.</summary>
    public bool HasContact { get; }

    /// <summary>Selected contact id, or <see cref="ContactCombatCardTokens.Unknown"/> when none is selected.</summary>
    public string ContactId { get; }

    /// <summary>Detection provenance read from the contact row, or the unknown token.</summary>
    public string Provenance { get; }

    /// <summary>Recorded freshness and age, or the unknown token.</summary>
    public string Freshness { get; }

    /// <summary>Recorded classification confidence, or the unknown token when provenance is missing.</summary>
    public string Confidence { get; }

    /// <summary>Caller-supplied posture label, or the unknown token.</summary>
    public string Posture { get; }

    /// <summary>Recorded targetability disposition and cause, or the unknown token.</summary>
    public string Targetability { get; }

    /// <summary>Latest recorded engagement row for an unambiguous target, or the unknown token.</summary>
    public string EngagementStatus { get; }

    /// <summary>Recorded BDA assess row, or the unknown token when no row exists.</summary>
    public string Bda { get; }

    /// <summary>Replay-stable fingerprint of this card.</summary>
    public string Fingerprint { get; }
}

/// <summary>Ordinal-sorted contact combat cards for a selection. Presentation-only.</summary>
public sealed class ContactCombatCardSnapshot
{
    /// <summary>No selected contacts.</summary>
    public static ContactCombatCardSnapshot Empty { get; } = new(
        Array.Empty<ContactCombatCard>(),
        ContactCombatCardTokens.EmptyFingerprint);

    /// <summary>Creates a snapshot. The card list is copied into a read-only wrapper.</summary>
    public ContactCombatCardSnapshot(IReadOnlyList<ContactCombatCard>? cards, string? fingerprint)
    {
        if (cards is null || cards.Count == 0)
        {
            Cards = Array.Empty<ContactCombatCard>();
        }
        else
        {
            var copy = new ContactCombatCard[cards.Count];
            for (var i = 0; i < cards.Count; i++)
            {
                copy[i] = cards[i];
            }

            Cards = Array.AsReadOnly(copy);
        }

        Fingerprint = string.IsNullOrEmpty(fingerprint)
            ? ContactCombatCardTokens.EmptyFingerprint
            : fingerprint;
    }

    /// <summary>Cards in ordinal contact-id order.</summary>
    public IReadOnlyList<ContactCombatCard> Cards { get; }

    /// <summary>Replay-stable fingerprint of <see cref="Cards"/>.</summary>
    public string Fingerprint { get; }
}

/// <summary>
/// Projects a selected contact into a combat card from read-only snapshots (DRG-169).
/// Reads provenance, targetability acceptance, BDA assess rows, and after-action ledger entries.
/// Does not recompute damage from the order log, infer posture, or mutate sim state
/// (ADR-010 §2–3, ADR-007, ADR-001).
/// </summary>
public static class ContactCombatCardProjection
{
    /// <summary>Builds one card. Null or blank <paramref name="contactId"/> yields <see cref="ContactCombatCard.Empty"/>.</summary>
    public static ContactCombatCard Project(
        string? contactId,
        ContactProvenanceSnapshot? provenance,
        TargetabilityAcceptSnapshot? targetability,
        BdaAssessSnapshot? assessments,
        AfterActionLedgerSnapshot? afterAction,
        string? postureLabel = null)
    {
        if (string.IsNullOrWhiteSpace(contactId))
        {
            return ContactCombatCard.Empty;
        }

        var provenanceKnown = TryFindUniqueProvenance(provenance, contactId, out var provenanceRow);
        var targetabilityKnown = TryFindUniqueTargetability(targetability, contactId, out var targetabilityRow);
        var bdaKnown = TryFindUniqueBda(assessments, contactId, out var bdaRow);
        var identityProvenance = provenanceKnown ? provenanceRow : null;
        var targetabilityAgrees = !targetabilityKnown
            || targetabilityRow is null
            || TargetAgrees(targetabilityRow.TargetId, identityProvenance, bdaKnown ? bdaRow?.TargetId : null);
        var bdaAgrees = !bdaKnown
            || bdaRow is null
            || TargetAgrees(bdaRow.TargetId, identityProvenance, targetabilityKnown ? targetabilityRow?.TargetId : null);
        var engagementTargetId = ResolveEngagementTargetId(
            identityProvenance,
            targetabilityKnown ? targetabilityRow : null,
            bdaKnown ? bdaRow : null);
        var engagement = SelectLatestEngagement(afterAction, engagementTargetId);

        return Create(
            contactId,
            provenanceKnown ? FormatProvenance(provenanceRow) : ContactCombatCardTokens.Unknown,
            provenanceKnown ? FormatFreshness(provenanceRow) : ContactCombatCardTokens.Unknown,
            provenanceKnown ? FormatConfidence(provenanceRow) : ContactCombatCardTokens.Unknown,
            FormatPosture(postureLabel),
            targetabilityKnown && targetabilityAgrees ? FormatTargetability(targetabilityRow) : ContactCombatCardTokens.Unknown,
            FormatEngagement(engagement),
            bdaKnown && bdaAgrees ? FormatBda(bdaRow) : ContactCombatCardTokens.Unknown);
    }

    /// <summary>
    /// Builds one card per distinct contact id, sorted ordinally.
    /// Input order does not affect the snapshot.
    /// </summary>
    public static ContactCombatCardSnapshot ProjectSelection(
        IReadOnlyList<string>? contactIds,
        ContactProvenanceSnapshot? provenance,
        TargetabilityAcceptSnapshot? targetability,
        BdaAssessSnapshot? assessments,
        AfterActionLedgerSnapshot? afterAction,
        IReadOnlyList<ContactCombatCardPostureFact>? postures = null)
    {
        if (contactIds is null || contactIds.Count == 0)
        {
            return ContactCombatCardSnapshot.Empty;
        }

        var ids = new List<string>(contactIds.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < contactIds.Count; i++)
        {
            var id = contactIds[i];
            if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
            {
                continue;
            }

            ids.Add(id);
        }

        if (ids.Count == 0)
        {
            return ContactCombatCardSnapshot.Empty;
        }

        ids.Sort(StringComparer.Ordinal);
        var cards = new ContactCombatCard[ids.Count];
        for (var i = 0; i < ids.Count; i++)
        {
            cards[i] = Project(
                ids[i],
                provenance,
                targetability,
                assessments,
                afterAction,
                ResolvePostureLabel(ids[i], postures));
        }

        return new ContactCombatCardSnapshot(cards, ComputeFingerprint(cards));
    }

    /// <summary>Replay-stable fingerprint. Blank selection is <see cref="ContactCombatCardTokens.EmptyFingerprint"/>.</summary>
    public static string ComputeFingerprint(ContactCombatCard? card)
    {
        if (card is null || !card.HasContact)
        {
            return ContactCombatCardTokens.EmptyFingerprint;
        }

        return FormatDetailedFingerprint(card);
    }

    /// <summary>True when the stored fingerprint matches a fresh compute of the same card.</summary>
    public static bool FingerprintMatches(ContactCombatCard? card) =>
        card is not null
        && string.Equals(card.Fingerprint, ComputeFingerprint(card), StringComparison.Ordinal);

    /// <summary>Replay-stable fingerprint for an ordinal card list.</summary>
    public static string ComputeFingerprint(IReadOnlyList<ContactCombatCard>? cards)
    {
        if (cards is null || cards.Count == 0)
        {
            return ContactCombatCardTokens.EmptyFingerprint;
        }

        var builder = new StringBuilder();
        builder.Append("ccc:n=");
        builder.Append(cards.Count.ToString(CultureInfo.InvariantCulture));
        for (var i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            builder.Append('|');
            builder.Append(card.Fingerprint);
        }

        return builder.ToString();
    }

    /// <summary>True when the stored snapshot fingerprint matches a fresh compute.</summary>
    public static bool FingerprintMatches(ContactCombatCardSnapshot? snapshot) =>
        snapshot is not null
        && string.Equals(snapshot.Fingerprint, ComputeFingerprint(snapshot.Cards), StringComparison.Ordinal);

    private static ContactCombatCard Create(
        string contactId,
        string provenance,
        string freshness,
        string confidence,
        string posture,
        string targetability,
        string engagementStatus,
        string bda)
    {
        var card = new ContactCombatCard(
            hasContact: true,
            contactId,
            provenance,
            freshness,
            confidence,
            posture,
            targetability,
            engagementStatus,
            bda,
            fingerprint: ContactCombatCardTokens.EmptyFingerprint);
        return new ContactCombatCard(
            card.HasContact,
            card.ContactId,
            card.Provenance,
            card.Freshness,
            card.Confidence,
            card.Posture,
            card.Targetability,
            card.EngagementStatus,
            card.Bda,
            FormatDetailedFingerprint(card));
    }

    private static string FormatDetailedFingerprint(ContactCombatCard card)
    {
        var builder = new StringBuilder();
        builder.Append("ccc:surface=");
        builder.Append(card.SurfaceKind);
        builder.Append(";has=");
        builder.Append(card.HasContact ? '1' : '0');
        builder.Append(";id=");
        builder.Append(card.ContactId);
        builder.Append(";prov=");
        builder.Append(card.Provenance);
        builder.Append(";fresh=");
        builder.Append(card.Freshness);
        builder.Append(";conf=");
        builder.Append(card.Confidence);
        builder.Append(";post=");
        builder.Append(card.Posture);
        builder.Append(";tgt=");
        builder.Append(card.Targetability);
        builder.Append(";eng=");
        builder.Append(card.EngagementStatus);
        builder.Append(";bda=");
        builder.Append(card.Bda);
        return builder.ToString();
    }

    private static string? ResolvePostureLabel(
        string contactId,
        IReadOnlyList<ContactCombatCardPostureFact>? postures)
    {
        if (postures is null || postures.Count == 0)
        {
            return null;
        }

        string? label = null;
        var found = false;
        for (var i = 0; i < postures.Count; i++)
        {
            var fact = postures[i];
            if (!string.Equals(fact.ContactId, contactId, StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(fact.Label))
            {
                continue;
            }

            if (!found)
            {
                label = fact.Label;
                found = true;
                continue;
            }

            if (!string.Equals(label, fact.Label, StringComparison.Ordinal))
            {
                return null;
            }
        }

        return found ? label : null;
    }

    private static bool TryFindUniqueProvenance(
        ContactProvenanceSnapshot? snapshot,
        string contactId,
        out ContactProvenanceState? row) =>
        TryFindUnique(snapshot?.Contacts, contactId, static candidate => candidate.ContactId, out row);

    private static bool TryFindUniqueTargetability(
        TargetabilityAcceptSnapshot? snapshot,
        string contactId,
        out TargetabilityAcceptContactRow? row) =>
        TryFindUnique(snapshot?.Contacts, contactId, static candidate => candidate.ContactId, out row);

    private static bool TryFindUniqueBda(
        BdaAssessSnapshot? snapshot,
        string contactId,
        out BdaAssessContactState? row) =>
        TryFindUnique(snapshot?.Contacts, contactId, static candidate => candidate.ContactId, out row);

    private static bool TryFindUnique<T>(
        IReadOnlyList<T>? rows,
        string contactId,
        Func<T, string> contactIdOf,
        out T? row)
        where T : class
    {
        row = null;
        if (rows is null)
        {
            return true;
        }

        var ambiguous = false;
        for (var i = 0; i < rows.Count; i++)
        {
            var candidate = rows[i];
            if (!string.Equals(contactIdOf(candidate), contactId, StringComparison.Ordinal))
            {
                continue;
            }

            if (row is null)
            {
                row = candidate;
                continue;
            }

            if (!row.Equals(candidate))
            {
                ambiguous = true;
            }
        }

        if (ambiguous)
        {
            row = null;
            return false;
        }

        return true;
    }

    /// <remarks>
    /// An assessment row keyed by the selected contact id may still describe another target.
    /// It renders only when its target matches every other non-empty target fact for the contact.
    /// </remarks>
    private static bool TargetAgrees(
        string? target,
        ContactProvenanceState? provenance,
        string? otherAssessmentTarget)
    {
        if (string.IsNullOrEmpty(target))
        {
            return true;
        }

        if (provenance is not null
            && (!SameOrEmpty(target, provenance.Source.TargetId)
                || !SameOrEmpty(target, provenance.LastKnown.TargetId)))
        {
            return false;
        }

        return SameOrEmpty(target, otherAssessmentTarget);
    }

    private static bool SameOrEmpty(string target, string? other) =>
        string.IsNullOrEmpty(other) || string.Equals(target, other, StringComparison.Ordinal);

    private static string? ResolveEngagementTargetId(
        ContactProvenanceState? provenance,
        TargetabilityAcceptContactRow? targetability,
        BdaAssessContactState? bda)
    {
        string? target = null;
        var count = 0;
        if (provenance is not null)
        {
            ConsiderTarget(ref target, ref count, provenance.Source.TargetId);
            ConsiderTarget(ref target, ref count, provenance.LastKnown.TargetId);
        }

        if (targetability is not null)
        {
            ConsiderTarget(ref target, ref count, targetability.TargetId);
        }

        if (bda is not null)
        {
            ConsiderTarget(ref target, ref count, bda.TargetId);
        }

        return count == 1 ? target : null;
    }

    private static void ConsiderTarget(ref string? target, ref int count, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (count == 0)
        {
            target = value;
            count = 1;
            return;
        }

        if (!string.Equals(target, value, StringComparison.Ordinal))
        {
            count = 2;
        }
    }

    /// <remarks>
    /// The ledger is emitted in combat-event order. Same-tick attempts are not re-sorted by
    /// correlation id, phase, or other fields; the last matching row is the latest engagement.
    /// </remarks>
    private static AfterActionLedgerEntry? SelectLatestEngagement(
        AfterActionLedgerSnapshot? afterAction,
        string? targetId)
    {
        if (afterAction is null || string.IsNullOrEmpty(targetId) || afterAction.Entries.Count == 0)
        {
            return null;
        }

        for (var i = afterAction.Entries.Count - 1; i >= 0; i--)
        {
            var entry = afterAction.Entries[i];
            if (string.Equals(entry.TargetId, targetId, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    private static string FormatProvenance(ContactProvenanceState? row)
    {
        if (row is null)
        {
            return ContactCombatCardTokens.Unknown;
        }

        var builder = new StringBuilder();
        builder.Append("obs=");
        builder.Append(row.Source.ObserverId);
        builder.Append(";tgt=");
        builder.Append(row.Source.TargetId);
        builder.Append(";ref=");
        builder.Append(row.Source.SourceRef);
        builder.Append(";life=");
        builder.Append(row.LastKnown.LifecycleState);
        builder.Append(";knownTgt=");
        builder.Append(row.LastKnown.TargetId);
        builder.Append(";quality=");
        builder.Append(row.QualityState);
        builder.Append(";ooc=");
        builder.Append(row.OutOfCommsUnknown ? '1' : '0');
        builder.Append(";tick=");
        builder.Append(row.LastKnown.LastSimTick.ToString(CultureInfo.InvariantCulture));
        builder.Append(";time=");
        builder.Append(row.LastKnown.LastSimTime.ToString("R", CultureInfo.InvariantCulture));
        return builder.ToString();
    }

    private static string FormatFreshness(ContactProvenanceState? row)
    {
        if (row is null)
        {
            return ContactCombatCardTokens.Unknown;
        }

        return row.Freshness + ";age=" + row.AgeTicks.ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatConfidence(ContactProvenanceState? row) =>
        row is null ? ContactCombatCardTokens.Unknown : row.Confidence.ToString();

    private static string FormatPosture(string? postureLabel) =>
        string.IsNullOrWhiteSpace(postureLabel) ? ContactCombatCardTokens.Unknown : postureLabel;

    private static string FormatTargetability(TargetabilityAcceptContactRow? row)
    {
        if (row is null)
        {
            return ContactCombatCardTokens.Unknown;
        }

        return row.Disposition + ";cause=" + row.WithheldCauseCode;
    }

    private static string FormatEngagement(AfterActionLedgerEntry? entry)
    {
        if (entry is null)
        {
            return ContactCombatCardTokens.Unknown;
        }

        var builder = new StringBuilder();
        builder.Append("shooter=");
        builder.Append(entry.ShooterId);
        builder.Append(";phase=");
        builder.Append(entry.Phase);
        builder.Append(";outcome=");
        builder.Append(entry.Outcome);
        builder.Append(";weapon=");
        builder.Append(entry.WeaponFamilyId);
        builder.Append(";tick=");
        builder.Append(entry.SimTick.ToString(CultureInfo.InvariantCulture));
        builder.Append(";time=");
        builder.Append(entry.SimTime.ToString("R", CultureInfo.InvariantCulture));
        builder.Append(";corr=");
        builder.Append(entry.CorrelationId.ToString(CultureInfo.InvariantCulture));
        builder.Append(";expl=");
        builder.Append(entry.ExplanationRef);
        return builder.ToString();
    }

    private static string FormatBda(BdaAssessContactState? row)
    {
        if (row is null)
        {
            return ContactCombatCardTokens.Unknown;
        }

        var builder = new StringBuilder();
        builder.Append("state=");
        builder.Append(row.State);
        builder.Append(";source=");
        builder.Append(row.Source);
        builder.Append(";tgt=");
        builder.Append(row.TargetId);
        builder.Append(";obs=");
        builder.Append(row.ObserverId);
        builder.Append(";tick=");
        builder.Append(row.SimTick.ToString(CultureInfo.InvariantCulture));
        builder.Append(";time=");
        builder.Append(row.SimTime.ToString("R", CultureInfo.InvariantCulture));
        builder.Append(";corr=");
        builder.Append(row.CorrelationSequenceId.ToString(CultureInfo.InvariantCulture));
        return builder.ToString();
    }
}
