namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>Why a unit in a group-order selection was excluded from fan-out.</summary>
public enum GroupOrderIneligibleReason
{
    /// <summary>Unit is eligible — never surfaced on an ineligible verdict.</summary>
    None,

    /// <summary>Unit was destroyed between selection and commit (GDD edge case: dropped silently, logged).</summary>
    Destroyed,

    /// <summary>Unit id no longer resolves to a registered/known unit.</summary>
    UnknownUnit,

    /// <summary>The requested order/attack option is not valid or not enabled for this unit.</summary>
    OptionNotAvailable,
}

/// <summary>Per-unit eligibility verdict for a prospective group order (req 20 §Selection, TR-c2-005).</summary>
public sealed record GroupOrderUnitVerdict(string UnitId, bool IsEligible, GroupOrderIneligibleReason Reason)
{
    public static GroupOrderUnitVerdict Eligible(string unitId) =>
        new(unitId, true, GroupOrderIneligibleReason.None);

    public static GroupOrderUnitVerdict Ineligible(string unitId, GroupOrderIneligibleReason reason) =>
        new(unitId, false, reason);
}

/// <summary>
/// Presentation-only plan for fanning a single group-order action out to every eligible unit in a
/// selection (req 20 §Selection, TR-c2-005). Pure/headless — never touches
/// <c>ProjectAegis.Delegation.UnityAdapter.Bridge.DelegationBridge</c> itself; the caller supplies an
/// <paramref name="evaluate"/> delegate built from live projections (OOB alive state, attack-menu
/// options, etc.) and this class only sequences the resulting verdicts.
/// </summary>
/// <remarks>
/// Callers must present <see cref="IneligibleUnits"/> (named, with reasons) to the player before
/// commit; <see cref="EligibleUnitIds"/> is the survivor list that <see cref="GroupOrderFanOut"/>
/// (or an equivalent thin loop) issues one bridge call per unit for.
/// </remarks>
public sealed class GroupOrderPlan
{
    private GroupOrderPlan(
        IReadOnlyList<string> eligibleUnitIds,
        IReadOnlyList<GroupOrderUnitVerdict> ineligibleUnits,
        IReadOnlyList<GroupOrderUnitVerdict> allVerdicts)
    {
        EligibleUnitIds = eligibleUnitIds;
        IneligibleUnits = ineligibleUnits;
        AllVerdicts = allVerdicts;
    }

    /// <summary>Unit ids to fan the order out to, in selection order.</summary>
    public IReadOnlyList<string> EligibleUnitIds { get; }

    /// <summary>Excluded units, named, with the reason each was dropped.</summary>
    public IReadOnlyList<GroupOrderUnitVerdict> IneligibleUnits { get; }

    /// <summary>Every verdict (eligible + ineligible) in selection order, for full audit/log display.</summary>
    public IReadOnlyList<GroupOrderUnitVerdict> AllVerdicts { get; }

    public bool HasAnyEligible => EligibleUnitIds.Count > 0;

    /// <summary>
    /// Build a plan by evaluating each id in <paramref name="selectedUnitIds"/> exactly once, in
    /// order. Duplicate ids in the input are only evaluated/reported once (mirrors
    /// <see cref="SelectionSet"/>'s de-duplication contract).
    /// </summary>
    public static GroupOrderPlan Build(
        IReadOnlyList<string> selectedUnitIds,
        Func<string, GroupOrderUnitVerdict?> evaluate)
    {
        if (evaluate == null)
        {
            throw new ArgumentNullException(nameof(evaluate));
        }

        var eligible = new List<string>();
        var ineligible = new List<GroupOrderUnitVerdict>();
        var all = new List<GroupOrderUnitVerdict>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var unitId in selectedUnitIds ?? Array.Empty<string>())
        {
            if (string.IsNullOrEmpty(unitId) || !seen.Add(unitId))
            {
                continue;
            }

            var verdict = evaluate(unitId);
            if (verdict == null)
            {
                // Defensive: a null evaluator result must not NRE the plan builder. Treat as unknown.
                verdict = GroupOrderUnitVerdict.Ineligible(unitId, GroupOrderIneligibleReason.UnknownUnit);
            }

            all.Add(verdict);

            if (verdict.IsEligible)
            {
                eligible.Add(verdict.UnitId);
            }
            else
            {
                ineligible.Add(verdict);
            }
        }

        return new GroupOrderPlan(eligible, ineligible, all);
    }
}
