using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using NUnit.Framework;

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

    [Test]
    public void Peer_on_same_side_receives_shared_detected_transition()
    {
        var merger = new DatalinkSidePictureMerger(SharingDoctrine, TwoObserverTrials);
        var organic = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        var shared = merger.Merge(organic, 1, 1.0);

        AssertEx.Single(shared);
        Assert.That(shared[0].ObserverId, Is.EqualTo("u2"));
        Assert.That(shared[0].ContactId, Is.EqualTo("dl-hostile-1"));
        Assert.That(shared[0].NewState, Is.EqualTo(ContactLifecycleState.Detected));
    }

    [Test]
    public void Organic_only_flag_suppresses_sharing()
    {
        var doctrine = SharingDoctrine with { OrganicOnly = true };
        var merger = new DatalinkSidePictureMerger(doctrine, TwoObserverTrials);
        var organic = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        Assert.That(merger.Merge(organic, 1, 1.0), Is.Empty);
    }

    [Test]
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

        Assert.That(merger.Merge(organic, 1, 1.0), Is.Empty);
    }

    [Test]
    public void Identical_merge_inputs_yield_identical_shared_sequence()
    {
        var a = RunSharedSequence();
        var b = RunSharedSequence();
        Assert.That(b.Count, Is.EqualTo(a.Count));
        for (var i = 0; i < a.Count; i++)
        {
            Assert.That(b[i], Is.EqualTo(a[i]));
        }
    }

    [Test]
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

        Assert.That(
            allShared.Any(t => t is
            {
                ObserverId: "u2",
                TargetId: "hostile-1",
                PreviousState: ContactLifecycleState.Detected,
                NewState: ContactLifecycleState.Classified,
            }),
            Is.True);
    }

    [Test]
    public void Duplicate_share_is_suppressed_on_subsequent_ticks()
    {
        var merger = new DatalinkSidePictureMerger(SharingDoctrine, TwoObserverTrials);
        var organic = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        AssertEx.Single(merger.Merge(organic, 1, 1.0));
        Assert.That(merger.Merge(Array.Empty<ContactTransition>(), 2, 2.0), Is.Empty);
    }

    [Test]
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

        Assert.That(shared.Count, Is.EqualTo(2));
        Assert.That(shared[0].TargetId, Is.EqualTo("hostile-a"));
        Assert.That(shared[1].TargetId, Is.EqualTo("hostile-b"));
    }

    [Test]
    public void Share_lag_zero_matches_immediate_peer_merge()
    {
        var doctrine = SharingDoctrine with { ShareLagTicks = 0 };
        var merger = new DatalinkSidePictureMerger(doctrine, TwoObserverTrials);
        var organic = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        var shared = merger.Merge(organic, 1, 1.0);

        AssertEx.Single(shared);
        Assert.That(shared[0].ObserverId, Is.EqualTo("u2"));
        Assert.That(shared[0].SimTick, Is.EqualTo((ulong)1));
        Assert.That(shared[0].NewState, Is.EqualTo(ContactLifecycleState.Detected));
    }

    [Test]
    public void Share_lag_defers_peer_merge_until_apply_tick()
    {
        var doctrine = SharingDoctrine with { ShareLagTicks = 2 };
        var merger = new DatalinkSidePictureMerger(doctrine, TwoObserverTrials);
        var organic = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        Assert.That(merger.Merge(organic, 1, 1.0), Is.Empty);
        Assert.That(merger.Merge(Array.Empty<ContactTransition>(), 2, 2.0), Is.Empty);

        var shared = merger.Merge(Array.Empty<ContactTransition>(), 3, 3.0);

        AssertEx.Single(shared);
        Assert.That(shared[0].ObserverId, Is.EqualTo("u2"));
        Assert.That(shared[0].SimTick, Is.EqualTo((ulong)3));
        Assert.That(shared[0].NewState, Is.EqualTo(ContactLifecycleState.Detected));
    }

    [Test]
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

        Assert.That(merger.Merge(Array.Empty<ContactTransition>(), 3, 3.0), Is.Empty);
    }

    [Test]
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

        Assert.That(b.Count, Is.EqualTo(a.Count));
        for (var i = 0; i < a.Count; i++)
        {
            Assert.That(b[i], Is.EqualTo(a[i]));
        }

        Assert.That(a.Count, Is.EqualTo(2));
        Assert.That(a[0].TargetId, Is.EqualTo("hostile-a"));
        Assert.That(a[1].TargetId, Is.EqualTo("hostile-b"));
    }

    [Test]
    public void Nominal_preserves_existing_peer_share()
    {
        var merger = new DatalinkSidePictureMerger(SharingDoctrine, TwoObserverTrials);
        var organic = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        var shared = merger.Merge(organic, 1, 1.0, DatalinkCommsShareState.Nominal);

        AssertEx.Single(shared);
        Assert.That(shared[0].ObserverId, Is.EqualTo("u2"));
        Assert.That(shared[0].NewState, Is.EqualTo(ContactLifecycleState.Detected));
    }

    [Test]
    public void Degraded_suppresses_new_peer_share()
    {
        var merger = new DatalinkSidePictureMerger(SharingDoctrine, TwoObserverTrials);
        var organic = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        Assert.That(merger.Merge(organic, 1, 1.0, DatalinkCommsShareState.Degraded), Is.Empty);
    }

    [Test]
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

        var peerLost = AssertEx.Single(shared, t => t.ObserverId == "u2");
        Assert.That(peerLost.NewState, Is.EqualTo(ContactLifecycleState.Lost));
    }

    [Test]
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

        AssertEx.Single(shared);
        Assert.That(shared[0].ObserverId, Is.EqualTo("u2"));
        Assert.That(shared[0].NewState, Is.EqualTo(ContactLifecycleState.Classified));
    }

    [Test]
    public void Denied_suppresses_all_shares()
    {
        var merger = new DatalinkSidePictureMerger(SharingDoctrine, TwoObserverTrials);
        var organic = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        Assert.That(merger.Merge(organic, 1, 1.0, DatalinkCommsShareState.Denied), Is.Empty);

        merger.Merge(organic, 1, 1.0);
        Assert.That(
            merger.Merge(
                    [
                        new ContactTransition(2, 2.0, "u1", "c1", "hostile-1", ContactLifecycleState.Detected, ContactLifecycleState.Classified),
                    ],
                    2,
                    2.0,
                    DatalinkCommsShareState.Denied),
            Is.Empty);
    }

    [Test]
    public void Default_merge_param_is_nominal()
    {
        var merger = new DatalinkSidePictureMerger(SharingDoctrine, TwoObserverTrials);
        var organic = new[]
        {
            new ContactTransition(1, 1.0, "u1", "c1", "hostile-1", ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        var shared = merger.Merge(organic, 1, 1.0);

        AssertEx.Single(shared);
        Assert.That(shared[0].ObserverId, Is.EqualTo("u2"));
        Assert.That(shared[0].NewState, Is.EqualTo(ContactLifecycleState.Detected));
    }

    [Test]
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
            Assert.That(merger.Merge(batch, tick, tick), Is.Empty);
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