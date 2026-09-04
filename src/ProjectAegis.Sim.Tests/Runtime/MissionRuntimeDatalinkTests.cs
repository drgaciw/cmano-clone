namespace ProjectAegis.Sim.Tests.Runtime;

using ProjectAegis.Sim.Runtime;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using Xunit;

public sealed class MissionRuntimeDatalinkTests
{
    private static readonly ScenarioDetectionTrial[] TwoObserverTrials =
    [
        new ScenarioDetectionTrial("u1", "radar-1", "hostile-1", "c1", 1.0),
        new ScenarioDetectionTrial("u2", "radar-2", "hostile-1", "c2", 0.0),
    ];

    private static readonly ScenarioDatalinkDoctrine SharingDoctrine = new(
        OrganicOnly: false,
        UnitSides: new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["u1"] = "blue",
            ["u2"] = "blue",
        });

    [Fact]
    public void Datalink_picture_ticks_with_nominal_comms()
    {
        var runtime = new MissionRuntime(SharingDoctrine, TwoObserverTrials);
        var organic = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        var merged = runtime.Tick(1, 1.0, organic, DatalinkCommsShareState.Nominal);

        // Organic + 1 shared peer transition = 2 transitions
        Assert.Equal(2, merged.Count);
        Assert.Single(runtime.LastOrganicTransitions);
        Assert.Single(runtime.LastSharedTransitions);

        var shared = runtime.LastSharedTransitions[0];
        Assert.Equal("u2", shared.ObserverId);
        Assert.Equal("dl-hostile-1", shared.ContactId);
        Assert.Equal(ContactLifecycleState.Detected, shared.NewState);
    }

    [Fact]
    public void Datalink_picture_suppressed_when_comms_denied()
    {
        var runtime = new MissionRuntime(SharingDoctrine, TwoObserverTrials);
        var organic = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        var merged = runtime.Tick(1, 1.0, organic, DatalinkCommsShareState.Denied);

        Assert.Single(merged);
        Assert.Single(runtime.LastOrganicTransitions);
        Assert.Empty(runtime.LastSharedTransitions);
        Assert.Equal(DatalinkCommsShareState.Denied, runtime.CommsState);
    }

    [Fact]
    public void Datalink_picture_with_share_lag_buffered_across_runtime_ticks()
    {
        // 2-tick share lag: detection on tick 1 should only appear at tick 1 + 2 = 3
        var doctrineWithLag = SharingDoctrine with { ShareLagTicks = 2 };
        var runtime = new MissionRuntime(doctrineWithLag, TwoObserverTrials);

        var organicTick1 = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        // Tick 1: organic reported; shared picture is empty due to share lag (not instantaneous)
        var merged1 = runtime.Tick(1, 1.0, organicTick1, DatalinkCommsShareState.Nominal);
        Assert.Single(merged1);
        Assert.Empty(runtime.LastSharedTransitions);

        // Tick 2: no organic detection; lag has not yet elapsed (ApplyTick is 3)
        var merged2 = runtime.Tick(2, 2.0);
        Assert.Empty(merged2);
        Assert.Empty(runtime.LastSharedTransitions);

        // Tick 3: lag expired; merger emits shared transition to peer u2
        var merged3 = runtime.Tick(3, 3.0);
        Assert.Single(merged3);
        Assert.Single(runtime.LastSharedTransitions);
        Assert.Equal("u2", runtime.LastSharedTransitions[0].ObserverId);
        Assert.Equal("dl-hostile-1", runtime.LastSharedTransitions[0].ContactId);
        Assert.Equal(ContactLifecycleState.Detected, runtime.LastSharedTransitions[0].NewState);
    }

    [Fact]
    public void Datalink_picture_recovers_when_comms_restored_from_denied_to_nominal()
    {
        var runtime = new MissionRuntime(SharingDoctrine, TwoObserverTrials);
        var organic = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        // Tick 1: comms Denied -> no sharing
        runtime.Tick(1, 1.0, organic, DatalinkCommsShareState.Denied);
        Assert.Empty(runtime.LastSharedTransitions);

        // Tick 2: comms restored to Nominal -> peer receives shared picture update
        runtime.Tick(2, 2.0, Array.Empty<ContactTransition>(), DatalinkCommsShareState.Nominal);
        Assert.Single(runtime.LastSharedTransitions);
        Assert.Equal("u2", runtime.LastSharedTransitions[0].ObserverId);
        Assert.Equal(ContactLifecycleState.Detected, runtime.LastSharedTransitions[0].NewState);
    }

    [Fact]
    public void Datalink_picture_degraded_comms_suppresses_initial_detection()
    {
        var runtime = new MissionRuntime(SharingDoctrine, TwoObserverTrials);
        var organic = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        // Degraded comms suppresses brand new unknown -> detected transitions
        runtime.Tick(1, 1.0, organic, DatalinkCommsShareState.Degraded);
        Assert.Empty(runtime.LastSharedTransitions);
    }

    [Fact]
    public void Play_mode_incremental_tick_advances_clock_and_ticks_datalink()
    {
        var runtime = new MissionRuntime(SharingDoctrine, TwoObserverTrials, fixedDeltaSeconds: 0.5);
        Assert.Equal(0UL, runtime.CurrentTick);
        Assert.Equal(0.0, runtime.CurrentSimTime);

        var organic = new[]
        {
            new ContactTransition(1, 0.5, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        // Tick 1 via parameterless delta advance
        runtime.Tick(organic);
        Assert.Equal(1UL, runtime.CurrentTick);
        Assert.Equal(0.5, runtime.CurrentSimTime);
        Assert.Single(runtime.LastSharedTransitions);

        // Tick 2 via parameterless delta advance
        runtime.Tick();
        Assert.Equal(2UL, runtime.CurrentTick);
        Assert.Equal(1.0, runtime.CurrentSimTime);
        Assert.Empty(runtime.LastSharedTransitions);
    }

    [Fact]
    public void Mission_timeline_events_fire_alongside_datalink_ticks()
    {
        var timeline = new ScenarioMissionTimeline(
            fireOrder: ["ev-patrol", "ev-strike"],
            events:
            [
                new ScenarioMissionEvent("ev-strike", 2, "MissionTransition", "STRIKE"),
                new ScenarioMissionEvent("ev-patrol", 1, "MissionTransition", "PATROL"),
            ]);

        var runtime = new MissionRuntime(
            SharingDoctrine,
            TwoObserverTrials,
            timeline: timeline);

        var organic = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        // Tick 1: ev-patrol fires, datalink picture updates
        runtime.Tick(1, 1.0, organic, DatalinkCommsShareState.Nominal);
        Assert.Single(runtime.LastFiredEvents);
        Assert.Equal("ev-patrol", runtime.LastFiredEvents[0].EventId);
        Assert.Single(runtime.LastSharedTransitions);

        // Tick 2: ev-strike fires
        runtime.Tick(2, 2.0);
        Assert.Single(runtime.LastFiredEvents);
        Assert.Equal("ev-strike", runtime.LastFiredEvents[0].EventId);

        // Tick 3: no more events
        runtime.Tick(3, 3.0);
        Assert.Empty(runtime.LastFiredEvents);
    }
}
