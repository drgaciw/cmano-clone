using ProjectAegis.Sim.Engage;
using Xunit;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class MvpEngagementDlzPersonalityTests
{
    [Fact]
    public void Early_personality_launches_in_approaching_band()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 2);
        var resolver = new MvpEngagementResolver(world, magazines);
        var request = new EngageRequest(1, 2, 0, 0);
        world.Set(request, new EngageContext(
            90_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            DlzPersonality: DlzPersonality.Early));

        var result = resolver.Resolve(request);
        Assert.True(result.Launched);
    }
}