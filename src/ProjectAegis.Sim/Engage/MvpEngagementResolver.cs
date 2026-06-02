namespace ProjectAegis.Sim.Engage;

/// <summary>MVP resolver: fire-control track, envelope/DLZ, magazine consumption.</summary>
public sealed class MvpEngagementResolver : IEngagementResolver
{
    private readonly IEngageWorldQuery _world;
    private readonly MagazineLedger _magazines;
    private ulong _nextEngagementId = 1;

    public MvpEngagementResolver(IEngageWorldQuery world, MagazineLedger magazines)
    {
        _world = world;
        _magazines = magazines;
    }

    public MagazineLedger Magazines => _magazines;

    public EngageResult Resolve(in EngageRequest request)
    {
        if (!_world.TryGetContext(request, out var ctx))
        {
            return EngageResult.Aborted(EngagementAbortReason.NoFireControlTrack);
        }

        if (!ctx.RadarEmconActive)
        {
            return EngageResult.Aborted(EngagementAbortReason.EmconOff);
        }

        if (!ctx.HasFireControlTrack)
        {
            return EngageResult.Aborted(EngagementAbortReason.NoFireControlTrack);
        }

        if (ctx.RoundsRemaining <= 0 && _magazines.GetRounds(request.ShooterUnitId, request.MountId) <= 0)
        {
            return EngageResult.Aborted(EngagementAbortReason.MagazineEmpty);
        }

        if (!ctx.Envelope.Contains(ctx.RangeMeters))
        {
            return EngageResult.Aborted(EngagementAbortReason.OutOfEnvelope);
        }

        if (DlzEvaluator.Evaluate(ctx.RangeMeters, ctx.Envelope) != DlzState.InZone)
        {
            return EngageResult.Aborted(EngagementAbortReason.DlzOut);
        }

        if (!_magazines.TryConsume(request.ShooterUnitId, request.MountId))
        {
            return EngageResult.Aborted(EngagementAbortReason.MagazineEmpty);
        }

        return EngageResult.Launch(_nextEngagementId++);
    }
}
