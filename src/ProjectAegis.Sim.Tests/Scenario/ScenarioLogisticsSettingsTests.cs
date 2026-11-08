using ProjectAegis.Sim.Scenario;
using Xunit;

namespace ProjectAegis.Sim.Tests.Scenario;

public sealed class ScenarioLogisticsSettingsTests
{
    [Fact]
    public void Constructor_ignores_out_of_order_fuel_fractions_when_burn_model_is_disabled()
    {
        // FuelCapacityKg/BurnRateKgPerSecond default to 0, so the fuel burn model is
        // disabled (UsesFuelBurnModel == false) and JokerFuelFraction/BingoFuelFraction are
        // inert per ResolveState/RemainingFuelFraction (both only read fractions when the
        // burn model is active). A scenario author who leaves the burn model unset but
        // supplies a BingoFuelFraction above the default JokerFuelFraction (e.g. a field
        // copy-pasted from another scenario, or staged ahead of enabling the burn model)
        // must not crash scenario load over fields that have no gameplay effect.
        var settings = new ScenarioLogisticsSettings(
            jokerSimSeconds: 300,
            bingoSimSeconds: 600,
            bingoFuelFraction: 0.30);

        Assert.False(settings.UsesFuelBurnModel);
        Assert.Equal(0.30, settings.BingoFuelFraction, 3);
    }

    [Fact]
    public void Constructor_still_enforces_fraction_ordering_when_burn_model_is_active()
    {
        // Regression guard: once the burn model is actually active (capacity + burn rate
        // both positive), the fractions are load-bearing (FuelLedger.ResolveBand /
        // RemainingFuelFraction), so the bingo <= joker invariant must still be enforced.
        Assert.Throws<ArgumentOutOfRangeException>(() => new ScenarioLogisticsSettings(
            jokerSimSeconds: 300,
            bingoSimSeconds: 600,
            fuelCapacityKg: 10_000,
            burnRateKgPerSecond: 80,
            bingoFuelFraction: 0.30));
    }
}
