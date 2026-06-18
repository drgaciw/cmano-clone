using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using Xunit;

namespace ProjectAegis.Sim.Tests.Sensors;

public sealed class DatalinkSidePictureMergerTests
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
    public void Peer_on_same_side_receives_shared_detected_transition()
    {
        var merger = new DatalinkSidePictureMerger(SharingDoctrine, TwoObserverTrials);
        var organic = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        var shared = merger.Merge(organic, 1, 1.0);

        Assert.Single(shared);
        Assert.Equal("u2", shared[0].ObserverId);
        Assert.Equal("dl-hostile-1", shared[0].ContactId);
        Assert.Equal(ContactLifecycleState.Detected, shared[0].NewState);
    }

    [Fact]
    public void Organic_only_flag_suppresses_sharing()
    {
        var doctrine = SharingDoctrine with { OrganicOnly = true };
        var merger = new DatalinkSidePictureMerger(doctrine, TwoObserverTrials);
        var organic = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        Assert.Empty(merger.Merge(organic, 1, 1.0));
    }

    [Fact]
    public void Observers_on_different_sides_do_not_share()
    {
        var doctrine = new ScenarioDatalinkDoctrine(
            OrganicOnly: false,
            UnitSides: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["u1"] = "blue",
                ["u2"] = "red",
            });
        var merger = new DatalinkSidePictureMerger(doctrine, TwoObserverTrials);
        var organic = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        Assert.Empty(merger.Merge(organic, 1, 1.0));
    }

    [Fact]
    public void Identical_merge_inputs_yield_identical_shared_sequence()
    {
        var a = RunSharedSequence();
        var b = RunSharedSequence();
        Assert.Equal(a.Count, b.Count);
        for (var i = 0; i < a.Count; i++)
        {
            Assert.Equal(a[i], b[i]);
        }
    }

    [Fact]
    public void Classify_promotion_is_shared_to_peer()
    {
        var lifecycle = new ScenarioContactLifecycle(
            StaleThresholdTicks: 10,
            ClassifyAfterTicks: 1,
            IdentifyAfterTicks: 0);
        var sim = new PdDetectionContactSimulator(
            SimSeed.FromScenario(42),
            TwoObserverTrials,
            contactLifecycle: lifecycle);
        var merger = new DatalinkSidePictureMerger(SharingDoctrine, TwoObserverTrials);

        var allShared = new List<ContactTransition>();
        for (ulong tick = 1; tick <= 2; tick++)
        {
            var organic = sim.Tick(tick, tick);
            allShared.AddRange(merger.Merge(organic, tick, tick));
        }

        Assert.Contains(
            allShared,
            t => t is
            {
                ObserverId: "u2",
                TargetId: "hostile-1",
                PreviousState: ContactLifecycleState.Detected,
                NewState: ContactLifecycleState.Classified,
            });
    }

    [Fact]
    public void Duplicate_share_is_suppressed_on_subsequent_ticks()
    {
        var merger = new DatalinkSidePictureMerger(SharingDoctrine, TwoObserverTrials);
        var organic = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        Assert.Single(merger.Merge(organic, 1, 1.0));
        Assert.Empty(merger.Merge(Array.Empty<ContactTransition>(), 2, 2.0));
    }

    [Fact]
    public void Shared_transitions_sort_by_observer_sensor_target()
    {
        var trials = new[]
        {
            new ScenarioDetectionTrial("u1", "radar-1", "hostile-b", "c-b", 1.0),
            new ScenarioDetectionTrial("u1", "radar-1", "hostile-a", "c-a", 1.0),
            new ScenarioDetectionTrial("u2", "radar-2", "hostile-a", "c2-a", 0.0),
            new ScenarioDetectionTrial("u2", "radar-2", "hostile-b", "c2-b", 0.0),
        };
        var merger = new DatalinkSidePictureMerger(SharingDoctrine, trials);
        var organic = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c-b", "hostile-b", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
            new ContactTransition(1, 1.0, "u1", "c-a", "hostile-a", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        var shared = merger.Merge(organic, 1, 1.0);

        Assert.Equal(2, shared.Count);
        Assert.Equal("hostile-a", shared[0].TargetId);
        Assert.Equal("hostile-b", shared[1].TargetId);
    }

    private static List<ContactTransition> RunSharedSequence()
    {
        var lifecycle = new ScenarioContactLifecycle(
            StaleThresholdTicks: 10,
            ClassifyAfterTicks: 1,
            IdentifyAfterTicks: 2);
        var sim = new PdDetectionContactSimulator(
            SimSeed.FromScenario(99),
            TwoObserverTrials,
            contactLifecycle: lifecycle);
        var merger = new DatalinkSidePictureMerger(SharingDoctrine, TwoObserverTrials);
        var allShared = new List<ContactTransition>();

        for (ulong tick = 1; tick <= 3; tick++)
        {
            var organic = sim.Tick(tick, tick);
            allShared.AddRange(merger.Merge(organic, tick, tick));
        }

        return allShared;
    }
}