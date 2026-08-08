namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;

/// <summary>
/// Thin live-editing contract façade (CMD-35).
/// Wraps <see cref="ScenarioEditCommandBus"/> / <see cref="ScenarioMutationResult"/> findings
/// into presentation state — does not reimplement validation or mutate the document.
/// </summary>
public static class LiveEditContract
{
    /// <summary>
    /// Project the latest bus report (and optional last-mutation outcome) for UI binding.
    /// Call after <see cref="ScenarioEditCommandBus.RefreshFindings"/> or a successful mutation.
    /// </summary>
    public static LiveEditSessionPresentation Project(
        ScenarioEditCommandBus bus,
        int editVersion,
        bool lastMutationOk = true,
        string? lastReason = null)
    {
        if (bus is null)
        {
            throw new ArgumentNullException(nameof(bus));
        }

        return LiveEditApplyState.FromValidationReport(
            bus.LastReport,
            editVersion,
            lastMutationOk,
            lastReason);
    }

    /// <summary>Project a mutation result returned by the command bus.</summary>
    public static LiveEditSessionPresentation Project(ScenarioMutationResult result) =>
        LiveEditApplyState.FromMutationResult(result);

    /// <summary>Project a raw validation report (e.g. after <see cref="ScenarioEditCommandBus.RefreshFindings"/>).</summary>
    public static LiveEditSessionPresentation Project(
        ValidationReport? report,
        int editVersion,
        bool lastMutationOk = true,
        string? lastReason = null) =>
        LiveEditApplyState.FromValidationReport(report, editVersion, lastMutationOk, lastReason);

    /// <summary>Refresh findings via the bus and project presentation (no document mutation).</summary>
    public static LiveEditSessionPresentation RefreshAndProject(
        ScenarioAuthoringSession session)
    {
        if (session is null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        var report = session.Bus.RefreshFindings();
        return LiveEditApplyState.FromValidationReport(
            report,
            session.EditVersion,
            lastMutationOk: true,
            lastReason: null);
    }

    /// <summary>Bind panel state from a mutation result (status line + codes + commit gate).</summary>
    public static LiveEditPanelState BindPanel(ScenarioMutationResult result) =>
        LiveEditPanelBinder.BindFromMutation(result);

    /// <summary>Bind panel state from an applied presentation.</summary>
    public static LiveEditPanelState BindPanel(LiveEditSessionPresentation presentation) =>
        LiveEditPanelBinder.Bind(presentation);
}
