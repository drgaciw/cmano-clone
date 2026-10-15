using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// DRG-407: memoizes the selected-unit combat detail and engage explanation that
/// <see cref="WeaponAbortTooltipBinder.BindForSelection"/> reads.
/// Presentation only (ADR-010 §2–3, ADR-007, ADR-001). Does not query the sim.
/// </summary>
/// <remarks>
/// The combat frame is keyed by reference. <c>LastCombatFrame</c> is replaced when the
/// picture is rebuilt, so a deep compare of the event log is not required on the frame path.
/// The engage preview is keyed by value because <c>ProjectSelectedEngagePreview</c> allocates
/// a new record on each read.
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
            && ReferenceEquals(_frame, frame)
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

    /// <summary>One cached right-unit combat bind. References stay stable across hits.</summary>
    public sealed record Result(
        EngageExplain Explain,
        CombatDetailPresentation Detail,
        WeaponAbortTooltipState Tooltip);
}
