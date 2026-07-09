namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Unit context action: open attack options for the current selection (req 20 §Context Menus).
/// Intent is logged only; host opens attack menu / engage path — no direct fire commit here.
/// </summary>
public sealed class AttackOptionsContextAction : IContextActionProvider
{
    public string ActionId => ContextActionIds.AttackOptions;

    public string Label => "Attack options";

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
            ContextActionIntentKinds.AttackOptions,
            context.SelectedUnitIds,
            Detail: context.PrimaryUnitId ?? string.Empty);
    }
}
