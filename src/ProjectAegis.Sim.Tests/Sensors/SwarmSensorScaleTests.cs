using ProjectAegis.Sim.Sensors;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Sensors;

public sealed class SwarmSensorScaleTests
{
    [Test]
    public void Half_integrity_swarm_detects_worse_than_full_under_controlled_fixture()
    {
        const double basePd = 0.8;
        const int max = 40;
        var fullPd = SwarmSensorScale.ScalePd(basePd, droneCount: max, maxDrones: max);
        var halfPd = SwarmSensorScale.ScalePd(basePd, droneCount: max / 2, maxDrones: max);

        Assert.That(
            Math.Round(fullPd, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(0.8, 6, MidpointRounding.AwayFromZero)));
        Assert.That(
            Math.Round(halfPd, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(0.4, 6, MidpointRounding.AwayFromZero)));
        Assert.That(halfPd < fullPd, Is.True);
    }

    [Test]
    public void Scale_factor_is_monotonic_in_drone_count()
    {
        const int max = 40;
        double? previous = null;
        for (var c = 0; c <= max; c++)
        {
            var s = SwarmSensorScale.ScaleFactor(c, max);
            if (previous is not null)
            {
                Assert.That(s >= previous.Value - 1e-12, Is.True);
            }

            previous = s;
        }
    }

    [Test]
    public void DetectionProbability_composes_swarm_scale_backward_compatible()
    {
        var baseline = DetectionProbability.ComputePd(0.5);
        Assert.That(
            Math.Round(baseline, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(0.5, 6, MidpointRounding.AwayFromZero)));

        var scaled = DetectionProbability.ComputePd(
            0.5,
            swarmIntegrityScale: SwarmSensorScale.ScaleFactor(20, 40));
        Assert.That(
            Math.Round(scaled, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(0.25, 6, MidpointRounding.AwayFromZero)));
        Assert.That(scaled < baseline, Is.True);
    }

    [Test]
    public void Zero_drones_yields_zero_pd_contribution()
    {
        Assert.That(SwarmSensorScale.ScalePd(0.9, 0, 40), Is.EqualTo(0));
        Assert.That(SwarmSensorScale.ScaleFactor(10, 0), Is.EqualTo(0));
    }
}
