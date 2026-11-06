using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.UnityAdapter.Bridge;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>Resolves explicit history inspection or the most recent event for the current selection.</summary>
public static class CombatSelectionPresenter
{
    /// <summary>Never changes command selection or creates a target association.</summary>
    public static CombatDetailPresentation Build(CombatPresentationFrame frame, string? key,
        string? selectedUnit, string? selectedContact)
    {
        var target = frame.Contacts.Contacts.FirstOrDefault(c => c.ContactId == selectedContact)?.TargetId;
        CombatEvent? chosen = null;
        for (var i = frame.Events.Events.Count - 1; i >= 0; i--)
        {
            var candidate = frame.Events.Events[i];
            if (key != null ? CombatMapPresenter.KeyFor(candidate) == key
                : target != null ? candidate.TargetId == target
                : selectedUnit != null && candidate.ShooterId == selectedUnit)
            {
                chosen = candidate;
                break;
            }
        }
        if (chosen == null)
        {
            if (target == null) return CombatDetailPresentation.Empty;
            var assessment = frame.Assessments.Contacts.FirstOrDefault(c => c.ContactId == selectedContact && c.TargetId == target);
            return CombatDetailPresentation.Empty with
            {
                StatusLine = "Engagement: no recorded engagement for this contact",
                BdaLine = assessment == null ? "BDA: UNKNOWN — no contact-specific assessment"
                    : $"BDA: {assessment.State} | Source: {assessment.Source}",
            };
        }
        // Keep observer-specific BDA scoped to the explicitly selected contact.
        return CombatDetailPresenter.Build(frame.Events, frame.Contacts, frame.Assessments,
            chosen.ShooterId, chosen.TargetId, chosen.CorrelationId,
            target == chosen.TargetId ? selectedContact : null, frame.Explanations);
    }
}
