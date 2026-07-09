namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Unit context action: plot course for the current selection (req 20 §Context Menus).
/// Emits a logged intent; host starts course-plot interaction via bridge commands.
/// </summary>
public sealed class PlotCourseContextAction : IContextActionProvider
{
    public string ActionId => ContextActionIds.PlotCourse;

    public string Label => "Plot course";

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
            ContextActionIntentKinds.PlotCourse,
            context.SelectedUnitIds,
            Detail: context.PrimaryUnitId ?? string.Empty);
    }
}
