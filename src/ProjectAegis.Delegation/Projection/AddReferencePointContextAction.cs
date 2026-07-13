namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Map-only context action: add reference point (req 20 §Context Menus / map context).
/// Eligible only with empty selection on the map surface. Host maps intent to
/// bridge <c>SetReferencePoint</c>-style commands (ADR-010).
/// </summary>
public sealed class AddReferencePointContextAction : IContextActionProvider
{
    public string ActionId => ContextActionIds.AddReferencePoint;

    public string Label => "Add reference point";

    public bool IsEligible(ContextActionEvaluationContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        return context.IsMapSurface && context.SelectedUnitIds.Count == 0;
    }

    public ContextActionIntent? CreateIntent(ContextActionEvaluationContext context)
    {
        if (context is null || !IsEligible(context))
        {
            return null;
        }

        return new ContextActionIntent(
            ActionId,
            ContextActionIntentKinds.AddReferencePoint,
            Array.Empty<string>());
    }
}
