namespace ProjectAegis.Data.Tests.Validation;

using ProjectAegis.Data.Validation;
using Xunit;

public sealed class ReachabilityCalculatorTests
{
    private const double Pad = 50;
    private const double FuelFrac = 0.85;

    [Fact]
    public void TryClassifyStrikeUnreachable_fuel_dominated_when_inside_combat_radius()
    {
        var ok = ReachabilityCalculator.TryClassifyStrikeUnreachable(
            distanceNm: 350,
            combatRadiusNm: 400,
            ingressEgressPadNm: Pad,
            fuelFraction: FuelFrac,
            out var excess,
            out var code);

        Assert.True(ok);
        Assert.Equal("STRIKE_UNREACHABLE_FUEL", code);
        Assert.Equal(60, excess, precision: 1);
    }

    [Fact]
    public void TryClassifyStrikeUnreachable_combat_radius_when_beyond_radius()
    {
        var ok = ReachabilityCalculator.TryClassifyStrikeUnreachable(
            distanceNm: 500,
            combatRadiusNm: 400,
            ingressEgressPadNm: Pad,
            fuelFraction: FuelFrac,
            out var excess,
            out var code);

        Assert.True(ok);
        Assert.Equal("STRIKE_UNREACHABLE", code);
        Assert.Equal(100, excess, precision: 1);
    }

    [Fact]
    public void IsReachable_true_when_within_fuel_budget()
    {
        Assert.True(ReachabilityCalculator.IsReachable(280, 400, Pad, FuelFrac, out _));
    }
}