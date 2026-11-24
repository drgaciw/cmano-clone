using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// DRG-268 ([C2-13]): Fog-safe result of clicking a message-log row to select a unit and open
/// the existing <see cref="EngageExplain"/> surface without introducing a new explain DTO.
/// </summary>
public sealed record MessageLogExplainLinkResult(
    bool HasExplain,
    ulong? SequenceId,
    string? SafeUnitIdToSelect,
    string? SafeContactIdToSelect,
    bool SuppressedByFog,
    EngageExplain Explain)
{
    public static MessageLogExplainLinkResult Empty { get; } = new(
        HasExplain: false,
        SequenceId: null,
        SafeUnitIdToSelect: null,
        SafeContactIdToSelect: null,
        SuppressedByFog: false,
        Explain: EngageExplain.Empty);
}

/// <summary>
/// DRG-268 ([C2-13]): Resolves message-log click-through to fog-safe unit/contact selection and
/// reuses <see cref="EngageExplain"/> for <c>EngageExplainPanelHost</c> (ADR-010 §2–3, ADR-007, ADR-001).
/// </summary>
public static class MessageLogExplainDeepLinkCoordinator
{
    /// <summary>
    /// Resolves a live <see cref="DecisionLogDeepLinkDetail"/> against player-visible friendly units and contacts.
    /// Unobserved entities are suppressed under fog and never selected or disclosed.
    /// </summary>
    public static MessageLogExplainLinkResult Resolve(
        DecisionLogDeepLinkDetail? detail,
        IReadOnlyCollection<string>? playerVisibleFriendlyIds,
        IReadOnlyCollection<string>? playerVisibleContactIds)
    {
        if (detail is null || !detail.Found)
        {
            return MessageLogExplainLinkResult.Empty;
        }

        var friendlies = new HashSet<string>(
            playerVisibleFriendlyIds ?? Array.Empty<string>(),
            StringComparer.Ordinal);
        var contacts = new HashSet<string>(
            playerVisibleContactIds ?? Array.Empty<string>(),
            StringComparer.Ordinal);

        string? safeUnit = null;
        string? safeContact = null;
        var hasCandidateEntity = false;

        var candidateUnit = !string.IsNullOrWhiteSpace(detail.UnitId) ? detail.UnitId : detail.ActorId;
        if (!string.IsNullOrWhiteSpace(candidateUnit))
        {
            hasCandidateEntity = true;
            if (friendlies.Contains(candidateUnit))
            {
                safeUnit = candidateUnit;
            }
            else if (contacts.Contains(candidateUnit))
            {
                safeContact = candidateUnit;
            }
        }

        for (var i = 0; i < detail.Fields.Count; i++)
        {
            var kv = detail.Fields[i];
            if (string.Equals(kv.Key, "TargetId", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(kv.Value))
            {
                if (contacts.Contains(kv.Value))
                {
                    safeContact = kv.Value;
                }
                else if (safeUnit is null && friendlies.Contains(kv.Value))
                {
                    safeUnit = kv.Value;
                }
            }
        }

        var suppressedByFog = hasCandidateEntity && safeUnit is null && safeContact is null;
        if (suppressedByFog)
        {
            var fogExplain = new EngageExplain(
                StatusLine: $"SEQ #{detail.SequenceId} — FOG: subject unobserved",
                ReasonCode: "FOG_UNOBSERVED",
                ReasonPlain: "Subject unit is not currently visible to player sensors; selection suppressed to preserve fog of war.",
                IsBlocked: true);

            return new MessageLogExplainLinkResult(
                HasExplain: true,
                SequenceId: detail.SequenceId,
                SafeUnitIdToSelect: null,
                SafeContactIdToSelect: null,
                SuppressedByFog: true,
                Explain: fogExplain);
        }

        var isBlocked = !string.IsNullOrWhiteSpace(detail.Rationale)
            && (detail.Rationale.Contains("Refus", StringComparison.OrdinalIgnoreCase)
                || detail.Rationale.Contains("Hold", StringComparison.OrdinalIgnoreCase)
                || detail.Rationale.Contains("Deny", StringComparison.OrdinalIgnoreCase)
                || detail.Rationale.Contains("Block", StringComparison.OrdinalIgnoreCase));

        var statusLine = string.IsNullOrWhiteSpace(detail.Category)
            ? $"SEQ #{detail.SequenceId} — {detail.Summary}"
            : $"SEQ #{detail.SequenceId} [{detail.Category}] — {detail.Summary}";

        var nextAction = safeUnit is not null
            ? $"Selected friendly unit '{safeUnit}'."
            : safeContact is not null
                ? $"Focused observed contact '{safeContact}'."
                : "Inspect order log entry.";

        var reasonPlain = string.IsNullOrWhiteSpace(detail.Rationale)
            ? $"{detail.Summary} — {nextAction}"
            : $"{detail.Summary} ({detail.Rationale}) — {nextAction}";

        var explain = new EngageExplain(
            StatusLine: statusLine,
            ReasonCode: detail.EntryKind?.ToString(),
            ReasonPlain: reasonPlain,
            IsBlocked: isBlocked);

        return new MessageLogExplainLinkResult(
            HasExplain: true,
            SequenceId: detail.SequenceId,
            SafeUnitIdToSelect: safeUnit,
            SafeContactIdToSelect: safeContact,
            SuppressedByFog: false,
            Explain: explain);
    }
}
