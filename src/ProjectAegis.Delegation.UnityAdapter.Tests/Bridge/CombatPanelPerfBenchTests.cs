using System.Diagnostics;
using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

public sealed class CombatPanelPerfBenchTests
{
    [Test]
    public void Combat_frame_map_and_detail_bind_1200_legs_within_100ms_budget()
    {
        var log = new DecisionLog();
        for (var i = 1; i <= 1200; i++)
            log.AppendEngagement(new EngagementRecord(0, i, (ulong)i, new TargetId("s"), (ulong)i, true,
                VictimTargetId: new TargetId("t"), WeaponFamilyId: "Gun", HasFireControlTrack: true));
        var symbols = MapPictureProjection.Project(new[] { new OobTreeEntry("s", true), new OobTreeEntry("t", true) },
            Array.Empty<ContactPictureEntry>(), 7);
        void Bind()
        {
            var frame = CombatPresentationFrameBridge.Build(log, SliceAContactFrame.Empty, 1200);
            var map = CombatMapPresenter.Build(frame.Events, symbols, frame.SimTime, CombatZoomBand.Tactical);
            var detail = CombatSelectionPresenter.Build(frame, null, "s", null);
            Assert.That(map.EventLines.Count, Is.EqualTo(3600));
            Assert.That(map.Effects.Count, Is.InRange(1, 64));
            Assert.That(detail.FiringSolutionLine, Does.Not.Contain("UNKNOWN"));
        }
        for (var i = 0; i < 3; i++) Bind();
        var samples = new double[20];
        for (var i = 0; i < samples.Length; i++)
        {
            var watch = Stopwatch.StartNew();
            Bind();
            samples[i] = watch.Elapsed.TotalMilliseconds;
        }
        Array.Sort(samples);
        TestContext.WriteLine($"Combat bind: n=20, p95={samples[18]:F3} ms, max={samples[19]:F3} ms");
        Assert.That(samples[18], Is.LessThan(100));
        Assert.That(samples[19], Is.LessThan(100));
    }
}
