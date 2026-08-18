namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless/Unity facade: tactical map symbols from snapshot alive-state + order-log contacts.
/// Read-only projection path (ADR-010 §2–3, ADR-007, ADR-001) — never mutates sim authority.
/// </summary>
public static class MapPictureBridge
{
    /// <summary>
    /// Builds an immutable map-symbol list for presentation bind (hosts / MapSymbolPool).
    /// Consumes <see cref="ISimWorldSnapshot"/> alive queries and a <see cref="DecisionLog"/>
    /// contact picture only — no live ECS / session write handles.
    /// When the snapshot publishes poses, symbols use those coordinates; otherwise hash fallback.
    /// </summary>
    /// <param name="snapshot">Read-only world snapshot (alive membership + optional kinematics).</param>
    /// <param name="registry">Target registry for OOB member ids.</param>
    /// <param name="log">Decision / order log (contact picture projection source).</param>
    /// <param name="layoutSeed">Deterministic layout seed for placeholder placement.</param>
    /// <returns>Immutable <see cref="IReadOnlyList{T}"/> of <see cref="MapSymbolEntry"/> rows.</returns>
    /// <exception cref="ArgumentNullException">When snapshot, registry, or log is null.</exception>
    public static IReadOnlyList<MapSymbolEntry> Build(
        ISimWorldSnapshot snapshot,
        TargetRegistry registry,
        DecisionLog log,
        int layoutSeed)
    {
        var (oob, contacts, poses) = CollectPictureInputs(snapshot, registry, log);
        return MapPictureProjection.Project(oob, contacts, layoutSeed, poses);
    }

    /// <summary>
    /// Builds plotted-course polylines from snapshot waypoint lists (CMD-38 / CMD-30.7).
    /// Presentation-only; does not enqueue orders or write DecisionLog.
    /// </summary>
    public static IReadOnlyList<MapCourseOverlayEntry> BuildCourses(
        ISimWorldSnapshot snapshot,
        TargetRegistry registry,
        DecisionLog log,
        int layoutSeed)
    {
        var (oob, contacts, poses) = CollectPictureInputs(snapshot, registry, log);
        _ = contacts;
        var courses = CollectCourses(snapshot, oob);
        return MapPictureProjection.ProjectCourses(oob, courses, poses, layoutSeed);
    }

    private static (
        IReadOnlyList<OobTreeEntry> Oob,
        IReadOnlyList<ContactPictureEntry> Contacts,
        IReadOnlyDictionary<string, UnitKinematicPose>? Poses)
        CollectPictureInputs(ISimWorldSnapshot snapshot, TargetRegistry registry, DecisionLog log)
    {
        // netstandard2.1 (Unity plugins): no ArgumentNullException.ThrowIfNull
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (registry is null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        var oob = OobTreeProjection.Project(registry.CollectMemberIds(), snapshot.IsMemberAlive);
        var contacts = ContactPictureProjection.Project(log);
        return (oob, contacts, CollectPoses(snapshot, oob, contacts));
    }

    private static IReadOnlyDictionary<string, UnitKinematicPose>? CollectPoses(
        ISimWorldSnapshot snapshot,
        IReadOnlyList<OobTreeEntry> oob,
        IReadOnlyList<ContactPictureEntry> contacts)
    {
        Dictionary<string, UnitKinematicPose>? poses = null;
        foreach (var unit in oob)
        {
            TryAddPose(snapshot, unit.UnitId, ref poses);
        }

        foreach (var contact in contacts)
        {
            TryAddPose(snapshot, contact.ContactId, ref poses);
        }

        return poses;
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<CourseWaypoint>>? CollectCourses(
        ISimWorldSnapshot snapshot,
        IReadOnlyList<OobTreeEntry> oob)
    {
        Dictionary<string, IReadOnlyList<CourseWaypoint>>? courses = null;
        foreach (var unit in oob)
        {
            var waypoints = snapshot.GetPlottedCourse(unit.UnitId);
            if (waypoints is null || waypoints.Count == 0)
            {
                continue;
            }

            courses ??= new Dictionary<string, IReadOnlyList<CourseWaypoint>>(StringComparer.Ordinal);
            courses[unit.UnitId] = waypoints;
        }

        return courses;
    }

    private static void TryAddPose(
        ISimWorldSnapshot snapshot,
        string id,
        ref Dictionary<string, UnitKinematicPose>? poses)
    {
        if (string.IsNullOrWhiteSpace(id) || !snapshot.TryGetKinematicPose(id, out var pose))
        {
            return;
        }

        poses ??= new Dictionary<string, UnitKinematicPose>(StringComparer.Ordinal);
        poses[id] = pose;
    }
}
