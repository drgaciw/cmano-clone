namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Req 20 P0 T6: unit context-menu action "Assign Mission". Registers through
/// <see cref="ContextActionRegistry"/> only — T2 menu shell is not edited (track isolation).
/// Emits a logged <see cref="ContextActionIntent"/>; host maps <see cref="IntentKind"/> to bridge.
/// </summary>
public sealed class AssignMissionContextActionProvider : IContextActionProvider
{
    /// <summary>Stable registry action id (Phase 0 contract example).</summary>
    public const string ActionIdValue = "assign_mission";

    /// <summary>Intent kind host maps to mission-assignment command path.</summary>
    public const string IntentKindValue = "assign_mission";

    public string ActionId => ActionIdValue;

    public string Label => "Assign Mission";

    /// <inheritdoc />
    public bool IsEligible(ContextActionEvaluationContext context)
    {
        if (context is null)
        {
            return false;
        }

        // Registry also hard-blocks replay; keep provider pure/local for direct checks.
        if (context.IsReplay)
        {
            return false;
        }

        return context.SelectedUnitIds.Count > 0;
    }

    /// <inheritdoc />
    public ContextActionIntent? CreateIntent(ContextActionEvaluationContext context)
    {
        if (!IsEligible(context))
        {
            return null;
        }

        return new ContextActionIntent(
            ActionId: ActionIdValue,
            IntentKind: IntentKindValue,
            TargetUnitIds: context.SelectedUnitIds);
    }
}

/// <summary>T6 registration helper — tracks register providers; shells only consume the registry.</summary>
public static class MissionRuntimeContextActions
{
    /// <summary>Registers T6 mission runtime context actions (assign mission). Idempotent per ActionId only if not already present.</summary>
    public static void RegisterDefaults(ContextActionRegistry registry)
    {
        if (registry is null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        if (registry.Providers.Any(p =>
                string.Equals(p.ActionId, AssignMissionContextActionProvider.ActionIdValue, StringComparison.Ordinal)))
        {
            return;
        }

        registry.Register(new AssignMissionContextActionProvider());
    }
}
