using ProjectAegis.Sim.Engage;
using Xunit;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class SwarmSalvoDeconflictionTests
{
    [Fact]
    public void Allocate_keeps_one_shooter_per_target_deterministically()
    {
        var requests = new[]
        {
            new SwarmSalvoDeconfliction.Slot(2, 100, 1),
            new SwarmSalvoDeconfliction.Slot(1, 100, 1),
            new SwarmSalvoDeconfliction.Slot(3, 200, 1),
        };

        var allocated = SwarmSalvoDeconfliction.Allocate(requests);

        Assert.Equal(2, allocated.Count);
        Assert.Equal((1ul, 100ul), (allocated[0].ShooterUnitId, allocated[0].TargetId));
        Assert.Equal((3ul, 200ul), (allocated[1].ShooterUnitId, allocated[1].TargetId));
    }
}