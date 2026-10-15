using ProjectAegis.Delegation.BdaAssess;
using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// DRG-407: memoizes the selected-unit combat detail and engage explanation that
/// <see cref="WeaponAbortTooltipBinder.BindForSelection"/> reads.
/// Presentation only (ADR-010 §2–3, ADR-007, ADR-001). Does not query the sim.
/// </summary>
/// <remarks>
/// <c>LastCombatFrame</c> is a new instance on every unpaused tick, and its sim clock
/// advances even when the rendered combat picture does not. The frame key is therefore
/// the content <see cref="WeaponAbortTooltipBinder.ScopeCombatDetail"/> and
/// <see cref="CombatDetailPresenter.Build"/> actually render: events, contact identity,
/// kill-chain presence, provenance confidence, BDA state and source, and engagement
/// explanations. Frame sim time, contact age, and other clocks stay out of the key.
/// The engage preview is keyed by value because <c>ProjectSelectedEngagePreview</c>
/// allocates a new record on each read.
/// </remarks>
public sealed class RightUnitCombatDetailCache
{
    private EngagePreview? _preview;
    private CombatPresentationFrame? _frame;
    private string? _selectedUnitId;
    private string? _selectedContactId;
    private bool _filled;
    private Result? _current;

    /// <summary>Times <see cref="Remember"/> stored a new bind. A hit does not increment this.</summary>
    public int MissCount { get; private set; }

    /// <summary>
    /// Returns the stored bind when the <see cref="WeaponAbortTooltipBinder.BindForSelection"/>
    /// inputs are unchanged.
    /// </summary>
    public bool TryGet(
        EngagePreview? preview,
        CombatPresentationFrame? frame,
        string? selectedUnitId,
        string? selectedContactId,
        out Result result)
    {
        if (_filled
            && _current is not null
            && SameCombatContent(_frame, frame)
            && string.Equals(_selectedUnitId, selectedUnitId, StringComparison.Ordinal)
            && string.Equals(_selectedContactId, selectedContactId, StringComparison.Ordinal)
            && SamePreview(_preview, preview))
        {
            result = _current;
            return true;
        }

        result = null!;
        return false;
    }

    /// <summary>Stores one freshly computed explain, scoped combat detail, and tooltip.</summary>
    public Result Remember(
        EngagePreview? preview,
        CombatPresentationFrame? frame,
        string? selectedUnitId,
        string? selectedContactId,
        EngageExplain explain,
        CombatDetailPresentation detail,
        WeaponAbortTooltipState tooltip)
    {
        if (explain is null)
        {
            throw new ArgumentNullException(nameof(explain));
        }

        if (detail is null)
        {
            throw new ArgumentNullException(nameof(detail));
        }

        if (tooltip is null)
        {
            throw new ArgumentNullException(nameof(tooltip));
        }

        _preview = preview;
        _frame = frame;
        _selectedUnitId = selectedUnitId;
        _selectedContactId = selectedContactId;
        _filled = true;
        MissCount++;
        _current = new Result(explain, detail, tooltip);
        return _current;
    }

    private static bool SamePreview(EngagePreview? left, EngagePreview? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Equals(right);
    }

    /// <summary>
    /// True when both frames would produce the same scoped combat detail.
    /// Ignores object identity and clocks that the detail card does not render.
    /// </summary>
    private static bool SameCombatContent(CombatPresentationFrame? left, CombatPresentationFrame? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return SameEvents(left.Events, right.Events)
            && SamePicture(left.Contacts, right.Contacts)
            && SameKillChain(left.Contacts, right.Contacts)
            && SameProvenance(left.Contacts, right.Contacts)
            && SameAssessments(left.Assessments, right.Assessments)
            && SameExplanations(left.Explanations, right.Explanations);
    }

    private static bool SameEvents(CombatEventSnapshot? left, CombatEventSnapshot? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return SameSequence(left.Events, right.Events);
    }

    private static bool SamePicture(SliceAContactFrame? left, SliceAContactFrame? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        return SameIds(left?.Contacts, right?.Contacts, static row => row.ContactId, static row => row.TargetId);
    }

    private static bool SameKillChain(SliceAContactFrame? left, SliceAContactFrame? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        return SameIds(
            left?.KillChain.Contacts,
            right?.KillChain.Contacts,
            static row => row.ContactId,
            static row => row.TargetId);
    }

    private static bool SameProvenance(SliceAContactFrame? left, SliceAContactFrame? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        var leftRows = left?.Provenance.Contacts;
        var rightRows = right?.Provenance.Contacts;
        var count = leftRows?.Count ?? 0;
        if (count != (rightRows?.Count ?? 0))
        {
            return false;
        }

        if (count == 0 || leftRows is null || rightRows is null)
        {
            return true;
        }

        for (var i = 0; i < count; i++)
        {
            if (leftRows[i].Confidence != rightRows[i].Confidence
                || !string.Equals(leftRows[i].ContactId, rightRows[i].ContactId, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool SameAssessments(BdaAssessSnapshot? left, BdaAssessSnapshot? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        var leftRows = left?.Contacts;
        var rightRows = right?.Contacts;
        var count = leftRows?.Count ?? 0;
        if (count != (rightRows?.Count ?? 0))
        {
            return false;
        }

        if (count == 0 || leftRows is null || rightRows is null)
        {
            return true;
        }

        for (var i = 0; i < count; i++)
        {
            var a = leftRows[i];
            var b = rightRows[i];
            if (a.State != b.State
                || a.Source != b.Source
                || !string.Equals(a.ContactId, b.ContactId, StringComparison.Ordinal)
                || !string.Equals(a.TargetId, b.TargetId, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool SameExplanations(
        IReadOnlyList<CombatEngagementExplanation>? left,
        IReadOnlyList<CombatEngagementExplanation>? right) =>
        SameSequence(left, right);

    private static bool SameIds<T>(
        IReadOnlyList<T>? left,
        IReadOnlyList<T>? right,
        Func<T, string> contactId,
        Func<T, string> targetId)
    {
        var count = left?.Count ?? 0;
        if (count != (right?.Count ?? 0))
        {
            return false;
        }

        if (count == 0 || left is null || right is null)
        {
            return true;
        }

        for (var i = 0; i < count; i++)
        {
            if (!string.Equals(contactId(left[i]), contactId(right[i]), StringComparison.Ordinal)
                || !string.Equals(targetId(left[i]), targetId(right[i]), StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool SameSequence<T>(IReadOnlyList<T>? left, IReadOnlyList<T>? right)
        where T : IEquatable<T>
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        var count = left?.Count ?? 0;
        if (count != (right?.Count ?? 0))
        {
            return false;
        }

        if (count == 0 || left is null || right is null)
        {
            return true;
        }

        for (var i = 0; i < count; i++)
        {
            if (!left[i].Equals(right[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>One cached right-unit combat bind. References stay stable across hits.</summary>
    public sealed record Result(
        EngageExplain Explain,
        CombatDetailPresentation Detail,
        WeaponAbortTooltipState Tooltip);
}
