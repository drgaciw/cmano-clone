namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Scenario;

/// <summary>
/// Headless/Unity facade: mission timeline events as presentation list rows.
/// Read-only projection path (ADR-010 §2–3, ADR-007, ADR-001) — never mutates sim authority.
/// </summary>
public static class MissionListBridge
{
    /// <summary>
    /// Projects a scenario mission timeline into immutable list entries for presentation bind.
    /// A null timeline is a valid empty picture (no events) — matches
    /// <see cref="MissionListProjection.Project"/>.
    /// </summary>
    /// <param name="timeline">Optional mission timeline from scenario policy; null → empty list.</param>
    /// <returns>Immutable <see cref="IReadOnlyList{T}"/> of <see cref="MissionListEntry"/> rows.</returns>
    public static IReadOnlyList<MissionListEntry> ProjectFrom(ScenarioMissionTimeline? timeline) =>
        MissionListProjection.Project(timeline);
}
