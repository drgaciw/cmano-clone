using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Time;
using NUnit.Framework;
using ProjectAegis.Sim.Tests;

namespace ProjectAegis.Sim.Tests.Core;

public sealed class SimTickPipelineTests
{
    [Test]
    public void Processes_enqueued_engagements_same_tick()
    {
        var resolver = new RecordingEngagementResolver();
        var pipeline = new SimTickPipeline(SimSeed.FromScenario(1), resolver);
        pipeline.EnqueueEngagement(new EngageRequest(10, 20, 0, 0));
        pipeline.TickOnce(TimeCompressionMode.RealTime);
        AssertEx.Single(resolver.Requests);
        AssertEx.Single(pipeline.LastEngagementResults);
        Assert.That(pipeline.LastEngagementResults[0].Launched, Is.True);
    }
}
