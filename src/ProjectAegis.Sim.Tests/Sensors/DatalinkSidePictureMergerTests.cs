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

    [Fact]
    public void Share_lag_zero_matches_immediate_peer_merge()
    {
        var doctrine = SharingDoctrine with { ShareLagTicks = 0 };
        var merger = new DatalinkSidePictureMerger(doctrine, TwoObserverTrials);
        var organic = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        var shared = merger.Merge(organic, 1, 1.0);

        Assert.Single(shared);
        Assert.Equal("u2", shared[0].ObserverId);
        Assert.Equal((ulong)1, shared[0].SimTick);
        Assert.Equal(ContactLifecycleState.Detected, shared[0].NewState);
    }

    [Fact]
    public void Share_lag_defers_peer_merge_until_apply_tick()
    {
        var doctrine = SharingDoctrine with { ShareLagTicks = 2 };
        var merger = new DatalinkSidePictureMerger(doctrine, TwoObserverTrials);
        var organic = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        Assert.Empty(merger.Merge(organic, 1, 1.0));
        Assert.Empty(merger.Merge(Array.Empty<ContactTransition>(), 2, 2.0));

        var shared = merger.Merge(Array.Empty<ContactTransition>(), 3, 3.0);

        Assert.Single(shared);
        Assert.Equal("u2", shared[0].ObserverId);
        Assert.Equal((ulong)3, shared[0].SimTick);
        Assert.Equal(ContactLifecycleState.Detected, shared[0].NewState);
    }

    [Fact]
    public void Share_lag_cancels_pending_share_when_contact_lost_before_apply_tick()
    {
        var doctrine = SharingDoctrine with { ShareLagTicks = 2 };
        var merger = new DatalinkSidePictureMerger(doctrine, TwoObserverTrials);

        merger.Merge(
        [
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        ],
        1,
        1.0);
        merger.Merge(
        [
            new ContactTransition(2, 2.0, "u1", "c1", "hostile-1", ContactLifecycleState.Detected, ContactLifecycleState.Lost),
        ],
        2,
        2.0);

        Assert.Empty(merger.Merge(Array.Empty<ContactTransition>(), 3, 3.0));
    }

    [Fact]
    public void Share_lag_deterministic_merge_order_preserved_after_deferral()
    {
        var trials = new[]
        {
            new ScenarioDetectionTrial("u1", "radar-1", "hostile-b", "c-b", 1.0),
            new ScenarioDetectionTrial("u1", "radar-1", "hostile-a", "c-a", 1.0),
            new ScenarioDetectionTrial("u2", "radar-2", "hostile-a", "c2-a", 0.0),
            new ScenarioDetectionTrial("u2", "radar-2", "hostile-b", "c2-b", 0.0),
        };
        var doctrine = SharingDoctrine with { ShareLagTicks = 1 };

        var a = RunLagSharedSequence(doctrine, trials);
        var b = RunLagSharedSequence(doctrine, trials);

        Assert.Equal(a.Count, b.Count);
        for (var i = 0; i < a.Count; i++)
        {
            Assert.Equal(a[i], b[i]);
        }

        Assert.Equal(2, a.Count);
        Assert.Equal("hostile-a", a[0].TargetId);
        Assert.Equal("hostile-b", a[1].TargetId);
    }

    [Fact]
    public void Nominal_preserves_existing_peer_share()
    {
        var merger = new DatalinkSidePictureMerger(SharingDoctrine, TwoObserverTrials);
        var organic = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        var shared = merger.Merge(organic, 1, 1.0, DatalinkCommsShareState.Nominal);

        Assert.Single(shared);
        Assert.Equal("u2", shared[0].ObserverId);
        Assert.Equal(ContactLifecycleState.Detected, shared[0].NewState);
    }

    [Fact]
    public void Degraded_suppresses_new_peer_share()
    {
        var merger = new DatalinkSidePictureMerger(SharingDoctrine, TwoObserverTrials);
        var organic = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        Assert.Empty(merger.Merge(organic, 1, 1.0, DatalinkCommsShareState.Degraded));
    }

    [Fact]
    public void Degraded_allows_lost_propagation()
    {
        var merger = new DatalinkSidePictureMerger(SharingDoctrine, TwoObserverTrials);
        merger.Merge(
        [
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        ],
        1,
        1.0,
        DatalinkCommsShareState.Nominal);

        var shared = merger.Merge(
        [
            new ContactTransition(2, 2.0, "u1", "c1", "hostile-1", ContactLifecycleState.Detected, ContactLifecycleState.Lost),
        ],
        2,
        2.0,
        DatalinkCommsShareState.Degraded);

        var peerLost = Assert.Single(shared, t => t.ObserverId == "u2");
        Assert.Equal(ContactLifecycleState.Lost, peerLost.NewState);
    }

    [Fact]
    public void Degraded_allows_existing_shared_state_updates()
    {
        var merger = new DatalinkSidePictureMerger(SharingDoctrine, TwoObserverTrials);
        merger.Merge(
        [
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        ],
        1,
        1.0,
        DatalinkCommsShareState.Nominal);

        var shared = merger.Merge(
        [
            new ContactTransition(2, 2.0, "u1", "c1", "hostile-1", ContactLifecycleState.Detected, ContactLifecycleState.Classified),
        ],
        2,
        2.0,
        DatalinkCommsShareState.Degraded);

        Assert.Single(shared);
        Assert.Equal("u2", shared[0].ObserverId);
        Assert.Equal(ContactLifecycleState.Classified, shared[0].NewState);
    }

    [Fact]
    public void Denied_suppresses_all_shares()
    {
        var merger = new DatalinkSidePictureMerger(SharingDoctrine, TwoObserverTrials);
        var organic = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        Assert.Empty(merger.Merge(organic, 1, 1.0, DatalinkCommsShareState.Denied));

        merger.Merge(organic, 1, 1.0, DatalinkCommsShareState.Nominal);
        Assert.Empty(merger.Merge(
            [
                new ContactTransition(2, 2.0, "u1", "c1", "hostile-1", ContactLifecycleState.Detected, ContactLifecycleState.Classified),
            ],
            2,
            2.0,
            DatalinkCommsShareState.Denied));
    }

    [Fact]
    public void Default_merge_param_is_nominal()
    {
        var merger = new DatalinkSidePictureMerger(SharingDoctrine, TwoObserverTrials);
        var organic = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        var shared = merger.Merge(organic, 1, 1.0);

        Assert.Single(shared);
        Assert.Equal("u2", shared[0].ObserverId);
        Assert.Equal(ContactLifecycleState.Detected, shared[0].NewState);
    }

    [Fact]
    public void Share_lag_exceeding_scenario_length_never_shares()
    {
        var doctrine = SharingDoctrine with { ShareLagTicks = 5 };
        var merger = new DatalinkSidePictureMerger(doctrine, TwoObserverTrials);
        var organic = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        for (ulong tick = 1; tick <= 3; tick++)
        {
            var batch = tick == 1 ? organic : Array.Empty<ContactTransition>();
            Assert.Empty(merger.Merge(batch, tick, tick));
        }
    }

    private static List<ContactTransition> RunLagSharedSequence(
        ScenarioDatalinkDoctrine doctrine,
        ScenarioDetectionTrial[] trials)
    {
        var merger = new DatalinkSidePictureMerger(doctrine, trials);
        var organic = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c-b", "hostile-b", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
            new ContactTransition(1, 1.0, "u1", "c-a", "hostile-a", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };
        var allShared = new List<ContactTransition>();

        merger.Merge(organic, 1, 1.0);
        allShared.AddRange(merger.Merge(Array.Empty<ContactTransition>(), 2, 2.0));

        return allShared;
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