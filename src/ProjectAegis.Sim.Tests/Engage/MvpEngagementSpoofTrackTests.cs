using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class MvpEngagementSpoofTrackTests
{
    [Test]
    public void Resolve_aborts_with_CYBER_SPOOF_TRACK_when_track_spoofed()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        var resolver = new MvpEngagementResolver(world, magazines, seed: SimSeed.FromScenario(1));
        var request = new EngageRequest(1, 2, MountId: 0, SimTick: 1);
        world.Set(
            request,
            new EngageContext(
                50_000,
                new WeaponEnvelope(1_000, 100_000),
                4,
                HasFireControlTrack: true,
                TrackSpoofed: true));

        var result = resolver.Resolve(in request);

        Assert.That(result.Launched, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(EngagementAbortReason.TrackSpoofed));
        Assert.That(EngagementAbortReasonCodes.ToLogCode(result.AbortReason), Is.EqualTo(AbortReasonCatalog.Cyber.CYBER_SPOOF_TRACK));
    }
}