using ProjectAegis.Data.Telemetry;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Telemetry;

public sealed class BalanceTelemetrySinkFactoryTests
{
    [Test]
    public void Default_feature_flags_return_no_op_sink()
    {
        var sink = BalanceTelemetrySinkFactory.Create();
        Assert.That(sink, Is.SameAs(NoOpBalanceTelemetrySink.Instance));
        Assert.That(new BalanceDriftFeatureFlags().EnableBalanceDrift, Is.False);
    }

    [Test]
    public void Enabled_feature_flag_returns_real_accumulator()
    {
        var sink = BalanceTelemetrySinkFactory.Create(new BalanceDriftFeatureFlags { EnableBalanceDrift = true });
        Assert.That(sink, Is.TypeOf<BalanceTelemetryAccumulator>());
    }
}