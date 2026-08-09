using ProjectAegis.Sim.Sensors;
using Xunit;

namespace ProjectAegis.Sim.Tests.Sensors;

public sealed class SwarmSensorScaleTests
{
    [Fact]
    public void Half_integrity_swarm_detects_worse_than_full_under_controlled_fixture()
    {
        const double basePd = 0.8;
        const int max = 40;
        var fullPd = SwarmSensorScale.ScalePd(basePd, droneCount: max, maxDrones: max);
        var halfPd = SwarmSensorScale.ScalePd(basePd, droneCount: max / 2, maxDrones: max);

        Assert.Equal(0.8, fullPd, 6);
        Assert.Equal(0.4, halfPd, 6);
        Assert.True(halfPd < fullPd);
    }

    [Fact]
    public void Scale_factor_is_monotonic_in_drone_count()
    {
        const int max = 40;
        double? previous = null;
        for (var c = 0; c <= max; c++)
        {
            var s = SwarmSensorScale.ScaleFactor(c, max);
            if (previous is not null)
            {
                Assert.True(s >= previous.Value - 1e-12);
            }

            previous = s;
        }
    }

    [Fact]
    public void DetectionProbability_composes_swarm_scale_backward_compatible()
    {
        var baseline = DetectionProbability.ComputePd(0.5);
        Assert.Equal(0.5, baseline, 6);

        var scaled = DetectionProbability.ComputePd(
            0.5,
            swarmIntegrityScale: SwarmSensorScale.ScaleFactor(20, 40));
        Assert.Equal(0.25, scaled, 6);
        Assert.True(scaled < baseline);
    }

    [Fact]
    public void Zero_drones_yields_zero_pd_contribution()
    {
        Assert.Equal(0, SwarmSensorScale.ScalePd(0.9, 0, 40));
        Assert.Equal(0, SwarmSensorScale.ScaleFactor(10, 0));
    }
}
