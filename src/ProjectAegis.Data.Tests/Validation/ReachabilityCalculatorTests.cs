namespace ProjectAegis.Data.Tests.Validation;

using ProjectAegis.Data.Validation;
using NUnit.Framework;

public sealed class ReachabilityCalculatorTests
{
    private const double Pad = 50;
    private const double FuelFrac = 0.85;

    [Test]
    public void TryClassifyStrikeUnreachable_fuel_dominated_when_inside_combat_radius()
    {
        var ok = ReachabilityCalculator.TryClassifyStrikeUnreachable(
            distanceNm: 350,
            combatRadiusNm: 400,
            ingressEgressPadNm: Pad,
            fuelFraction: FuelFrac,
            out var excess,
            out var code);

        Assert.That(ok, Is.True);
        Assert.That(code, Is.EqualTo("STRIKE_UNREACHABLE_FUEL"));
        Assert.That(excess, Is.EqualTo(60).Within(0.05));
    }

    [Test]
    public void TryClassifyStrikeUnreachable_combat_radius_when_beyond_radius()
    {
        var ok = ReachabilityCalculator.TryClassifyStrikeUnreachable(
            distanceNm: 500,
            combatRadiusNm: 400,
            ingressEgressPadNm: Pad,
            fuelFraction: FuelFrac,
            out var excess,
            out var code);

        Assert.That(ok, Is.True);
        Assert.That(code, Is.EqualTo("STRIKE_UNREACHABLE"));
        Assert.That(excess, Is.EqualTo(100).Within(0.05));
    }

    [Test]
    public void IsReachable_true_when_within_fuel_budget()
    {
        Assert.That(ReachabilityCalculator.IsReachable(280, 400, Pad, FuelFrac, out _), Is.True);
    }
}