namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Unit context action: formation controls for the current selection (req 20 §Context Menus).
/// Emits a logged intent; host maps to formation/group commands.
/// </summary>
public sealed class FormationContextAction : IContextActionProvider
{
    public string ActionId => ContextActionIds.Formation;

    public string Label => "Formation";

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
            ContextActionIntentKinds.Formation,
            context.SelectedUnitIds,
            Detail: context.PrimaryUnitId ?? string.Empty);
    }
}
