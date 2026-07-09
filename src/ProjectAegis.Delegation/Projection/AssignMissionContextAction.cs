namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Unit context action: assign mission (Req 20 P0 T2 stub for T6).
/// Emits <see cref="ContextActionIntentKinds.AssignMissionStub"/> only — real mission
/// activate/deactivate is owned by T6 mission runtime entry. No modal-only path (AC-6).
/// </summary>
public sealed class AssignMissionContextAction : IContextActionProvider
{
    public string ActionId => ContextActionIds.AssignMission;

    public string Label => "Assign mission";

    public bool IsEligible(ContextActionEvaluationContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        return context.SelectedUnitIds.Count > 0;
    }

    public ContextActionIntent? CreateIntent(ContextActionEvaluationContext context)
    {
        if (context is null || context.SelectedUnitIds.Count == 0)
        {
            return null;
        }

        return new ContextActionIntent(
            ActionId,
            ContextActionIntentKinds.AssignMissionStub,
            context.SelectedUnitIds,
            Detail: "stub");
    }
}
