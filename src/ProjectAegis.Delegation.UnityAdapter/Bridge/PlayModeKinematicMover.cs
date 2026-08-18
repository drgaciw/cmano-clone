namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless Play Mode kinematic stub (CMD-38). Advances ORBAT icons from course/speed
/// on a small smoke theater so motion is visible without a Baltic ECS world.
/// Presentation/snapshot publisher only — not DelegationBridge hotpath.
/// </summary>
public sealed class PlayModeKinematicMover
{
    /// <summary>Smoke theater width (nm). Smaller than Baltic 800 nm so 22 kt cruise is visible.</summary>
    public const float DefaultTheaterWidthNm = 4f;

    public const float DefaultCruiseKnots = 22f;

    public const float PlotLookaheadNormalized = 0.22f;

    private readonly Dictionary<string, UnitKinematicPose> _poses = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IReadOnlyList<CourseWaypoint>> _courses = new(StringComparer.Ordinal);
    private readonly HashSet<string> _halted = new(StringComparer.Ordinal);

    public int TrackedCount => _poses.Count;

    public void EnsureSeeded(string unitId, int layoutSeed, bool hostile)
    {
        if (string.IsNullOrWhiteSpace(unitId) || _poses.ContainsKey(unitId))
        {
            return;
        }

        var (x, y) = MapPictureProjection.Place(unitId, hostile ? layoutSeed + 17 : layoutSeed);
        var course = hostile ? 225f : 45f;
        _poses[unitId] = new UnitKinematicPose(
            Latitude: null,
            Longitude: null,
            NormalizedX: x,
            NormalizedY: y,
            CourseDeg: course,
            SpeedNmPerHour: DefaultCruiseKnots);
    }

    public void Advance(double dtSeconds, IReadOnlyCollection<string>? destroyedIds = null)
    {
        if (dtSeconds <= 0)
        {
            return;
        }

        var hours = dtSeconds / 3600.0;
        var ids = _poses.Keys.ToArray();
        foreach (var id in ids)
        {
            if (destroyedIds is not null && ContainsOrdinal(destroyedIds, id))
            {
                continue;
            }

            if (_halted.Contains(id))
            {
                continue;
            }

            var pose = _poses[id];
            if (pose.SpeedNmPerHour <= 0f || pose.NormalizedX is not float x || pose.NormalizedY is not float y)
            {
                continue;
            }

            if (_courses.TryGetValue(id, out var waypoints) && waypoints.Count > 0)
            {
                var dest = waypoints[waypoints.Count - 1];
                pose = SteerToward(pose, dest);
                var next = Step(pose, x, y, hours);
                if (Reached(next.NormalizedX!.Value, next.NormalizedY!.Value, dest))
                {
                    next = next with
                    {
                        NormalizedX = dest.NormalizedX,
                        NormalizedY = dest.NormalizedY,
                        SpeedNmPerHour = 0f,
                    };
                    _courses.Remove(id);
                    _halted.Add(id);
                }

                _poses[id] = next;
                continue;
            }

            _poses[id] = Step(pose, x, y, hours);
        }
    }

    public void PlotCourseAhead(string unitId)
    {
        if (!_poses.TryGetValue(unitId, out var pose)
            || pose.NormalizedX is not float x
            || pose.NormalizedY is not float y)
        {
            return;
        }

        _halted.Remove(unitId);
        var rad = pose.CourseDeg * (Math.PI / 180.0);
        var destX = ClampCanvas(x + (PlotLookaheadNormalized * (float)Math.Sin(rad)));
        var destY = ClampCanvas(y - (PlotLookaheadNormalized * (float)Math.Cos(rad)));
        _courses[unitId] =
        [
            new CourseWaypoint(x, y),
            new CourseWaypoint(destX, destY),
        ];
        _poses[unitId] = pose with { SpeedNmPerHour = DefaultCruiseKnots };
    }

    public void Halt(string unitId)
    {
        _courses.Remove(unitId);
        _halted.Add(unitId);
        if (_poses.TryGetValue(unitId, out var pose))
        {
            _poses[unitId] = pose with { SpeedNmPerHour = 0f };
        }
    }

    public bool TryGetPose(string unitOrContactId, out UnitKinematicPose pose) =>
        _poses.TryGetValue(unitOrContactId, out pose);

    public IReadOnlyList<CourseWaypoint>? GetCourse(string unitId) =>
        _courses.TryGetValue(unitId, out var waypoints) ? waypoints : null;

    private static UnitKinematicPose SteerToward(UnitKinematicPose pose, CourseWaypoint dest)
    {
        if (pose.NormalizedX is not float x || pose.NormalizedY is not float y)
        {
            return pose;
        }

        var dx = dest.NormalizedX - x;
        var dy = dest.NormalizedY - y;
        if (Math.Abs(dx) < 1e-8f && Math.Abs(dy) < 1e-8f)
        {
            return pose;
        }

        // 0° = north (−Y), 90° = east (+X)
        var course = (float)(Math.Atan2(dx, -dy) * (180.0 / Math.PI));
        if (course < 0f)
        {
            course += 360f;
        }

        return pose with { CourseDeg = course };
    }

    private static UnitKinematicPose Step(UnitKinematicPose pose, float x, float y, double hours)
    {
        var rad = pose.CourseDeg * (Math.PI / 180.0);
        var distanceNorm = (float)(pose.SpeedNmPerHour * hours / DefaultTheaterWidthNm);
        var nx = ClampCanvas(x + (distanceNorm * (float)Math.Sin(rad)));
        var ny = ClampCanvas(y - (distanceNorm * (float)Math.Cos(rad)));
        return pose with { NormalizedX = nx, NormalizedY = ny };
    }

    private static bool Reached(float x, float y, CourseWaypoint dest) =>
        Math.Abs(x - dest.NormalizedX) <= 0.01f && Math.Abs(y - dest.NormalizedY) <= 0.01f;

    private static float ClampCanvas(float v) => v < 0.05f ? 0.05f : v > 0.95f ? 0.95f : v;

    private static bool ContainsOrdinal(IReadOnlyCollection<string> ids, string id)
    {
        foreach (var candidate in ids)
        {
            if (string.Equals(candidate, id, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
