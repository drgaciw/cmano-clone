using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Time;
using Xunit;

namespace ProjectAegis.Sim.Tests.Core;

public sealed class SimTickPipelineTests
{
    [Fact]
    public void Processes_enqueued_engagements_same_tick()
    {
        var resolver = new RecordingEngagementResolver();
        var pipeline = new SimTickPipeline(SimSeed.FromScenario(1), resolver);
        pipeline.EnqueueEngagement(new EngageRequest(10, 20, 0, 0));
        pipeline.TickOnce(TimeCompressionMode.RealTime);
        Assert.Single(resolver.Requests);
        Assert.Single(pipeline.LastEngagementResults);
        Assert.True(pipeline.LastEngagementResults[0].Launched);
    }
}
