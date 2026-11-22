using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Sim.Scenario;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// DRG-370: read-only projection adapter bridging <see cref="DelegationBridge"/> to
/// <see cref="CommitConstraintStripBinder"/> and <see cref="CommitRefusalBarkBinder"/>
/// so Unity panel hosts never import <c>ProjectAegis.Sim</c> or query sim internals directly
/// (ADR-010 §2–3, ADR-007, ADR-001).
/// </summary>
public static class RightUnitCommitProjection
{
    private const string DefaultOptionId = "fire-single";

    /// <summary>
    /// Projects the pre-commit constraint + magazine cost strip for the selected unit and armed option.
    /// Returns <see cref="CommitConstraintStripState.Empty"/> when selection or preview is unavailable.
    /// </summary>
    public static CommitConstraintStripState ProjectStrip(
        DelegationBridge? bridge,
        string? selectedUnitId,
        EngagePreview? engagePreview,
        AttackOptionsPreviewState? attackMenu,
        WraSalvoRemainingState? wra,
        WeaponAbortTooltipState? abortTooltip,
        string? armedOptionId = null)
    {
        if (engagePreview is null || string.IsNullOrWhiteSpace(selectedUnitId))
        {
            return CommitConstraintStripState.Empty;
        }

        var optionId = ResolveOptionId(attackMenu, armedOptionId);
        var engageDefaults = bridge?.Orchestrator.ScenarioPolicy?.EngageDefaults
            ?? ScenarioEngageDefaults.MvpFallback;
        var ctx = engageDefaults.ToEngageContext(engageDefaults.DefaultMagazineRounds);
        return CommitConstraintStripBinder.Bind(
            optionId,
            in ctx,
            engagePreview,
            attackMenu,
            wra,
            abortTooltip);
    }

    /// <summary>
    /// Projects the most recent dropped commit bark for <paramref name="selectedUnitId"/> from the
    /// bridge's read-only order log, coalescing correlated policy denials and synthetic engagement aborts.
    /// </summary>
    public static CommitRefusalBark? ProjectLatestDroppedBark(
        DelegationBridge? bridge,
        string? selectedUnitId)
    {
        if (bridge is null || string.IsNullOrWhiteSpace(selectedUnitId))
        {
            return null;
        }

        var entries = bridge.Orchestrator.DecisionLog.ChronologicalEntries();
        var barks = CommitRefusalBarkBinder.ProjectDropped(entries, selectedUnitId);
        return barks.Count > 0 ? barks[barks.Count - 1] : null;
    }

    private static string ResolveOptionId(AttackOptionsPreviewState? attackMenu, string? armedOptionId)
    {
        if (!string.IsNullOrWhiteSpace(armedOptionId))
        {
            return armedOptionId!;
        }

        if (attackMenu is { Rows.Count: > 0 })
        {
            if (AttackOptionsPreviewBinder.FindRow(attackMenu, DefaultOptionId) is not null)
            {
                return DefaultOptionId;
            }

            return attackMenu.Rows[0].OptionId;
        }

        return DefaultOptionId;
    }
}
