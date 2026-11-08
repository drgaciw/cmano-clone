using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class FuelBandHostContractTests
{
    [Test]
    public void Unit_detail_host_binds_fuel_band_cue_classes()
    {
        var source = UiIaSourceReader.ReadRuntime("RightUnitPanelHost.cs");
        Assert.That(source, Does.Contain("UnitDetailFuelBandBinder.Bind"));
        Assert.That(source, Does.Contain("FuelBandCueClasses"));
        Assert.That(source, Does.Contain("ApplyFuelBandCueClass"));
        Assert.That(source, Does.Contain("fuel-line"));
    }

    [Test]
    public void Message_log_host_binds_fuel_band_cue_classes_on_rows()
    {
        var source = UiIaSourceReader.ReadRuntime("MessageLogPanelHost.cs");
        Assert.That(source, Does.Contain("MessageLogFuelBandBinder.Bind"));
        Assert.That(source, Does.Contain("FuelBandCueClasses.All"));
        Assert.That(source, Does.Not.Contain("AppendFuelStateChange"));
        Assert.That(source, Does.Not.Contain("FuelTimelineTracker"));
    }
}
