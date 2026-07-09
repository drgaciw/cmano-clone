namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Registers T2 core context-menu actions into a <see cref="ContextActionRegistry"/>.
/// T3/T4/T6 append their own providers; they must not remove or reorder core entries.
/// </summary>
public static class CoreContextActionRegistration
{
    /// <summary>
    /// Registration order for unit + map core actions (CMO-aligned menu order).
    /// </summary>
    public static readonly string[] CoreActionIds =
    [
        ContextActionIds.AttackOptions,
        ContextActionIds.PlotCourse,
        ContextActionIds.Formation,
        ContextActionIds.AssignMission,
        ContextActionIds.MeasureDistance,
        ContextActionIds.AddReferencePoint,
    ];

    /// <summary>Registers the six core providers in stable order.</summary>
    public static void RegisterAll(ContextActionRegistry registry)
    {
        if (registry is null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        registry.Register(new AttackOptionsContextAction());
        registry.Register(new PlotCourseContextAction());
        registry.Register(new FormationContextAction());
        registry.Register(new AssignMissionContextAction());
        registry.Register(new MeasureDistanceContextAction());
        registry.Register(new AddReferencePointContextAction());
    }

    /// <summary>Creates a registry pre-loaded with core context actions.</summary>
    public static ContextActionRegistry CreateRegistryWithCoreActions()
    {
        var registry = new ContextActionRegistry();
        RegisterAll(registry);
        return registry;
    }

    /// <summary>Creates a <see cref="ContextMenuShell"/> with core actions registered.</summary>
    public static ContextMenuShell CreateShellWithCoreActions() =>
        new(CreateRegistryWithCoreActions());
}
