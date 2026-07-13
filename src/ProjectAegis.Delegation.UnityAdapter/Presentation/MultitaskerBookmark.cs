namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// Engine-agnostic camera pose for multitasker bookmarks (req 20 §Simulation Controls / CMO §1.3).
/// Pure value — no UnityEngine types so headless tests can round-trip without the Editor.
/// </summary>
/// <param name="LongitudeDegrees">WGS84 longitude in degrees.</param>
/// <param name="LatitudeDegrees">WGS84 latitude in degrees.</param>
/// <param name="HeightMeters">Ellipsoid height (or orthographic altitude) in meters.</param>
/// <param name="HeadingDegrees">Camera heading / yaw in degrees (0 = north).</param>
/// <param name="PitchDegrees">Camera pitch in degrees (negative = look down).</param>
public readonly record struct CameraPose(
    double LongitudeDegrees,
    double LatitudeDegrees,
    double HeightMeters,
    float HeadingDegrees,
    float PitchDegrees);

/// <summary>
/// Saved multitasker context: camera pose + selection unit ids + optional agent-pause flag
/// (req 20 P0 Multitasker mode). Presentation-only (ADR-010) — never sim state.
/// </summary>
/// <param name="Id">Stable slot key (hotkey slot or user-assigned label).</param>
/// <param name="Camera">Camera pose at capture time.</param>
/// <param name="SelectionUnitIds">Ordered friendly unit ids in the selection set.</param>
/// <param name="AgentPaused">
/// Optional agent decision-loop pause flag restored with the bookmark (null = leave agent state alone).
/// T4 owns agent-gate actuation; this flag is the presentation snapshot only.
/// </param>
public sealed record MultitaskerBookmark(
    string Id,
    CameraPose Camera,
    IReadOnlyList<string> SelectionUnitIds,
    bool? AgentPaused = null);
