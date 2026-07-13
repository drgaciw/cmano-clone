using ProjectAegis.Data.Telemetry;
using Xunit;

namespace ProjectAegis.Data.Tests.Telemetry;

public sealed class BalanceTelemetrySinkFactoryTests
{
    [Fact]
    public void Default_feature_flags_return_no_op_sink()
    {
        var sink = BalanceTelemetrySinkFactory.Create();
        Assert.Same(NoOpBalanceTelemetrySink.Instance, sink);
        Assert.False(new BalanceDriftFeatureFlags().EnableBalanceDrift);
    }

    [Fact]
    public void Enabled_feature_flag_returns_real_accumulator()
    {
        var sink = BalanceTelemetrySinkFactory.Create(new BalanceDriftFeatureFlags { EnableBalanceDrift = true });
        Assert.IsType<BalanceTelemetryAccumulator>(sink);
    }
}