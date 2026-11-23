namespace ProjectAegis.Delegation.UnityAdapter.Save;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Bridge;
using Core;

public sealed record AppliedOrderRecord(
    EntityKey Entity,
    TargetId Target,
    OrderId OrderId,
    OrderKind Kind,
    RiskLevel Risk,
    double SimTime);

/// <summary>
/// H6-04 / DRG-349: Replayable simulation world exposing <see cref="ISimWorldSnapshot"/> and
/// a genuine side-effect-applying <see cref="IOrderSink"/> (kinematic courses/poses, EMCON/sensor/EW
/// postures, craft ops counters, and applied order log).
/// </summary>
public sealed class ReplayableSimWorld : ISimWorldSnapshot, IOrderSink
{
    private readonly List<AppliedOrderRecord> _appliedOrders = new();
    private readonly Dictionary<string, string> _emconPostures = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _sensorPostures = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _ewPostures = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _craftOpsCounts = new(StringComparer.Ordinal);
    private readonly PlayModeKinematicMover _kinematicMover = new();

    private ISimWorldSnapshot? _innerSnapshot;
    private IOrderSink? _additionalInnerSink;
    private bool _forwardOrdersToInnerSink;
    private bool _isApplyingOrder;
    private TargetRegistry? _registry;
    private int _layoutSeed = 1;
    private double _standaloneSimTime;
    private int _standaloneContactCount;
    private bool _standaloneHasTrack;
    private TargetId? _standalonePrimaryHostileId;

    public IReadOnlyList<AppliedOrderRecord> AppliedOrders => _appliedOrders;

    public IReadOnlyDictionary<string, string> EmconPostures => _emconPostures;

    public IReadOnlyDictionary<string, string> SensorPostures => _sensorPostures;

    public IReadOnlyDictionary<string, string> EwPostures => _ewPostures;

    public IReadOnlyDictionary<string, int> CraftOpsCounts => _craftOpsCounts;

    public PlayModeKinematicMover KinematicMover => _kinematicMover;

    public void BindInnerSnapshot(
        ISimWorldSnapshot innerSnapshot,
        TargetRegistry? registry = null,
        int layoutSeed = 1,
        IOrderSink? additionalInnerSink = null,
        bool forwardOrdersToInnerSink = false)
    {
        _innerSnapshot = innerSnapshot ?? throw new ArgumentNullException(nameof(innerSnapshot));
        _registry = registry;
        _layoutSeed = layoutSeed;
        _additionalInnerSink = ReferenceEquals(additionalInnerSink, this) ? null : additionalInnerSink;
        _forwardOrdersToInnerSink = forwardOrdersToInnerSink;
    }

    public void BindSnapshotState(
        double simTime,
        int contactCount,
        bool hasFireControlTrack,
        TargetId? primaryHostileId = null)
    {
        _standaloneSimTime = simTime;
        _standaloneContactCount = contactCount;
        _standaloneHasTrack = hasFireControlTrack;
        _standalonePrimaryHostileId = primaryHostileId;
    }

    public void AdvanceTo(double simTime) => _standaloneSimTime = simTime;

    public void SeedUnitPose(string unitId, int layoutSeed, bool hostile = false)
    {
        _layoutSeed = layoutSeed;
        _kinematicMover.EnsureSeeded(unitId, layoutSeed, hostile);
    }

    public void AdvanceKinematics(double dtSeconds, IReadOnlyCollection<string>? destroyedIds = null) =>
        _kinematicMover.Advance(dtSeconds, destroyedIds);

    public double SimTime => _innerSnapshot != null ? _innerSnapshot.SimTime : _standaloneSimTime;

    public int ContactCount => _innerSnapshot != null ? _innerSnapshot.ContactCount : _standaloneContactCount;

    public int ActiveEngagementCount => _innerSnapshot?.ActiveEngagementCount ?? 0;

    public bool IsMemberAlive(TargetId memberId) =>
        _innerSnapshot != null ? _innerSnapshot.IsMemberAlive(memberId) : true;

    public TargetId? PrimaryHostileContactId =>
        _innerSnapshot != null ? _innerSnapshot.PrimaryHostileContactId : _standalonePrimaryHostileId;

    public bool HasFireControlTrackOnPrimaryContact =>
        _innerSnapshot != null ? _innerSnapshot.HasFireControlTrackOnPrimaryContact : _standaloneHasTrack;

    public bool ObserverRadarEmconActive =>
        _innerSnapshot != null ? _innerSnapshot.ObserverRadarEmconActive : true;

    public bool PrimaryHostileDestroyed => _innerSnapshot?.PrimaryHostileDestroyed ?? false;

    public TargetId? PrimaryBlueForceContactId => _innerSnapshot?.PrimaryBlueForceContactId;

    public bool PrimaryBlueForceContactDestroyed => _innerSnapshot?.PrimaryBlueForceContactDestroyed ?? false;

    public IReadOnlyDictionary<string, string>? PreferredHostileByShooter =>
        _innerSnapshot?.PreferredHostileByShooter;

    public bool TryGetKinematicPose(string unitOrContactId, out UnitKinematicPose pose) =>
        _kinematicMover.TryGetPose(unitOrContactId, out pose);

    public IReadOnlyList<CourseWaypoint>? GetPlottedCourse(string unitId) =>
        _kinematicMover.GetCourse(unitId);

    public void ApplyOrder(EntityKey entity, in Order order)
    {
        if (_isApplyingOrder)
        {
            return;
        }

        _isApplyingOrder = true;
        try
        {
            var unitId = order.Target.Value;
            if (string.IsNullOrWhiteSpace(unitId) && _registry != null && _registry.TryGetBinding(entity, out var binding))
            {
                unitId = binding.TargetId.Value;
            }

            _appliedOrders.Add(new AppliedOrderRecord(
                entity,
                order.Target,
                order.Id,
                order.Kind,
                order.Risk,
                order.SimTime));

            if (!string.IsNullOrWhiteSpace(unitId))
            {
                _kinematicMover.EnsureSeeded(unitId, _layoutSeed, hostile: false);
                switch (order.Kind)
                {
                    case OrderKind.Move:
                        _kinematicMover.PlotCourseAhead(unitId);
                        break;
                    case OrderKind.Hold:
                    case OrderKind.ReturnToBase:
                        _kinematicMover.Halt(unitId);
                        break;
                    case OrderKind.SetEmcon:
                        _emconPostures[unitId] = "active";
                        break;
                    case OrderKind.SetSensors:
                        _sensorPostures[unitId] = "active";
                        break;
                    case OrderKind.SetEwPosture:
                        _ewPostures[unitId] = "jam";
                        break;
                    case OrderKind.LaunchAircraft:
                    case OrderKind.AbortLaunchAircraft:
                    case OrderKind.LaunchBoat:
                    case OrderKind.RecoverBoat:
                    case OrderKind.AbortBoatLaunch:
                        _craftOpsCounts[unitId] = _craftOpsCounts.TryGetValue(unitId, out var c) ? c + 1 : 1;
                        break;
                }
            }

            var sinkToForward = _additionalInnerSink
                ?? (_forwardOrdersToInnerSink ? _innerSnapshot as IOrderSink : null);
            if (sinkToForward != null && !ReferenceEquals(sinkToForward, this))
            {
                sinkToForward.ApplyOrder(entity, in order);
            }
        }
        finally
        {
            _isApplyingOrder = false;
        }
    }

    public string ComputeSideEffectsDigestSha256()
    {
        var sb = new StringBuilder();
        sb.Append("orders=").Append(_appliedOrders.Count).Append('\n');
        foreach (var o in _appliedOrders)
        {
            sb.Append(o.Entity.Value).Append('|')
                .Append(o.Target.Value).Append('|')
                .Append(o.OrderId.Value).Append('|')
                .Append((int)o.Kind).Append('|')
                .Append((int)o.Risk).Append('|')
                .Append(o.SimTime.ToString("R", CultureInfo.InvariantCulture)).Append('\n');
        }

        AppendMap(sb, "emcon", _emconPostures);
        AppendMap(sb, "sensors", _sensorPostures);
        AppendMap(sb, "ew", _ewPostures);
        foreach (var kv in _craftOpsCounts.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            sb.Append("craft:").Append(kv.Key).Append('=').Append(kv.Value).Append('\n');
        }

        if (_registry != null)
        {
            foreach (var b in _registry.Bindings.OrderBy(x => x.TargetId.Value, StringComparer.Ordinal))
            {
                if (_kinematicMover.TryGetPose(b.TargetId.Value, out var pose))
                {
                    sb.Append("pose:").Append(b.TargetId.Value).Append(':')
                        .Append(pose.NormalizedX?.ToString("R", CultureInfo.InvariantCulture) ?? "").Append(',')
                        .Append(pose.NormalizedY?.ToString("R", CultureInfo.InvariantCulture) ?? "").Append(',')
                        .Append(pose.CourseDeg.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                        .Append(pose.SpeedNmPerHour.ToString("R", CultureInfo.InvariantCulture)).Append('\n');
                }
            }
        }

        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        var chars = new char[hash.Length * 2];
        for (var i = 0; i < hash.Length; i++)
        {
            var b = hash[i];
            chars[i * 2] = (char)((b >> 4) < 10 ? '0' + (b >> 4) : 'a' + ((b >> 4) - 10));
            chars[i * 2 + 1] = (char)((b & 0xF) < 10 ? '0' + (b & 0xF) : 'a' + ((b & 0xF) - 10));
        }

        return new string(chars);
    }

    private static void AppendMap(StringBuilder sb, string label, Dictionary<string, string> map)
    {
        foreach (var kv in map.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            sb.Append(label).Append(':').Append(kv.Key).Append('=').Append(kv.Value).Append('\n');
        }
    }
}
