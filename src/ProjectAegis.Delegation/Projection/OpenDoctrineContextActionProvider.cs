namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Req 20 P0 T3: unit context-menu action that opens the doctrine / EMCON / WRA panel.
/// Registered via <see cref="ContextActionRegistry"/> — menu shell (T2) only renders and invokes.
/// ADR-010: eligibility is pure; invoke yields a logged intent for the host (never a sim mutation).
/// </summary>
public sealed class OpenDoctrineContextActionProvider : IContextActionProvider
{
    public const string ActionIdValue = "open_doctrine";
    public const string IntentKindValue = "OpenDoctrinePanel";

    /// <inheritdoc />
    public string ActionId => ActionIdValue;

    /// <inheritdoc />
    public string Label => "Doctrine / EMCON / WRA";

    /// <inheritdoc />
    public bool IsEligible(ContextActionEvaluationContext context)
    {
        if (context is null)
        {
            return false;
        }

        if (context.IsReplay)
        {
            return false;
        }

        return context.SelectedUnitIds.Count > 0 || !string.IsNullOrWhiteSpace(context.PrimaryUnitId);
    }

    /// <inheritdoc />
    public ContextActionIntent? CreateIntent(ContextActionEvaluationContext context)
    {
        if (context is null || !IsEligible(context))
        {
            return null;
        }

        var targets = context.SelectedUnitIds.Count > 0
            ? context.SelectedUnitIds
            : (IReadOnlyList<string>)(string.IsNullOrWhiteSpace(context.PrimaryUnitId)
                ? Array.Empty<string>()
                : new[] { context.PrimaryUnitId! });

        if (targets.Count == 0)
        {
            return null;
        }

        var detail = context.PrimaryUnitId ?? targets[0];
        return new ContextActionIntent(ActionIdValue, IntentKindValue, targets, detail);
    }
}
