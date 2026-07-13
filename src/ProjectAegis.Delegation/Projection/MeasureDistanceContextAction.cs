namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Map-only context action: measure distance (req 20 §Context Menus / map context).
/// Eligible only with empty selection on the map surface.
/// </summary>
public sealed class MeasureDistanceContextAction : IContextActionProvider
{
    public string ActionId => ContextActionIds.MeasureDistance;

    public string Label => "Measure distance";

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
            ContextActionIntentKinds.MeasureDistance,
            Array.Empty<string>());
    }
}
