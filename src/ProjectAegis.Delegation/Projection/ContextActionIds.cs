namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Stable context-action ids and intent kinds for Req 20 P0 context menus (T2).
/// Tracks (T3/T4/T6) must reuse these constants when registering additional providers.
/// </summary>
public static class ContextActionIds
{
    public const string AttackOptions = "attack_options";
    public const string PlotCourse = "plot_course";
    public const string Formation = "formation";
    public const string AssignMission = "assign_mission";
    public const string MeasureDistance = "measure_distance";
    public const string AddReferencePoint = "add_reference_point";
}

/// <summary>
/// Intent-kind strings emitted by core context actions. Host maps these to bridge APIs
/// (ADR-010). <see cref="AssignMission"/> is a T2 stub; T6 owns real mission activate.
/// </summary>
public static class ContextActionIntentKinds
{
    public const string AttackOptions = "attack_options";
    public const string PlotCourse = "plot_course";
    public const string Formation = "formation";
    /// <summary>Stub kind for assign-mission until T6 wires mission runtime activate.</summary>
    public const string AssignMissionStub = "assign_mission_stub";
    public const string MeasureDistance = "measure_distance";
    public const string AddReferencePoint = "add_reference_point";
}
