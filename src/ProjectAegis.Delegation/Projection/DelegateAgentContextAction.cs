namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Req 20 P0 / ADR-019: context-menu action <c>delegate_agent</c> registered via
/// <see cref="IContextActionProvider"/>. Host maps the intent to bridge commands
/// (<see cref="AutonomyLevelChangeRequested"/> / release-to-agent) — never mutates sim tables here.
/// </summary>
public sealed class DelegateAgentContextAction : IContextActionProvider
{
    /// <summary>Stable action id for menu shells and intent logs.</summary>
    public const string ActionIdValue = "delegate_agent";

    /// <summary>Intent kind payload the host routes to the ADR-019 bridge surface.</summary>
    public const string IntentKindValue = "DelegateAgent";

    /// <inheritdoc />
    public string ActionId => ActionIdValue;

    /// <inheritdoc />
    public string Label => "Delegate agent";

    /// <inheritdoc />
    public bool IsEligible(ContextActionEvaluationContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.IsReplay)
        {
            return false;
        }

        return context.SelectedUnitIds.Count > 0;
    }

    /// <inheritdoc />
    public ContextActionIntent? CreateIntent(ContextActionEvaluationContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (!IsEligible(context))
        {
            return null;
        }

        return new ContextActionIntent(
            ActionId,
            IntentKind: IntentKindValue,
            TargetUnitIds: context.SelectedUnitIds,
            Detail: nameof(Core.AutonomyLevel.FullAutonomous));
    }

    /// <summary>Register this action on a shared registry (T2 menu shell consumes it).</summary>
    public static void Register(ContextActionRegistry registry)
    {
        if (registry is null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        registry.Register(new DelegateAgentContextAction());
    }
}
