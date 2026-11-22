// PlayMode acceptance smoke test for DRG-239 ([HYG-05] smoke-scene task-group + coverage fixture).
// Proves CoverageMapView binds the live smoke provider's authored normalized [0..1] polygons
// cleanly in Play Mode with 0 errors and exercises the command facade for group Hold/Withdraw.
//
// Note: Enables owner Play Mode signoff for DRG-175, DRG-185, DRG-192, DRG-235, and DRG-244;
// does NOT replace DRG-208 owner acceptance.
#if UNITY_5_3_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;
using ProjectAegis.Unity.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Tests
{
    /// <summary>
    /// Play Mode smoke tests verifying <see cref="CoverageMapView"/> binds live authored polygons
    /// from <see cref="PlayModeSmokeCoordinationFactsProvider"/> with zero errors (DRG-239).
    /// Enables owner signoff (DRG-175/185/192/235/244); does NOT replace DRG-208 owner acceptance.
    /// </summary>
    public sealed class SmokeTaskGroupCoveragePlayModeTests
    {
        private sealed class SmokeWorldStub : ISimWorldSnapshot, IOrderSink
        {
            private readonly HashSet<string> _aliveIds;

            public SmokeWorldStub(IEnumerable<string> aliveIds)
            {
                _aliveIds = new HashSet<string>(aliveIds, System.StringComparer.Ordinal);
            }

            public double SimTime { get; set; }
            public int ContactCount => 1;
            public int ActiveEngagementCount => 0;
            public TargetId? PrimaryHostileContactId => new TargetId(PlayModeSmokeOrbatSeeder.HostileUnitId);
            public bool HasFireControlTrackOnPrimaryContact => true;
            public bool ObserverRadarEmconActive => true;
            public bool IsMemberAlive(TargetId memberId) => _aliveIds.Contains(memberId.Value);

            public List<(EntityKey Entity, Order Order)> Applied { get; } = new();
            public void ApplyOrder(EntityKey entity, in Order order) => Applied.Add((entity, order));
        }

        [UnityTest]
        public IEnumerator CoverageMapView_binds_live_smoke_provider_polygons_in_play_mode_with_zero_errors()
        {
            var go = new GameObject("smoke-task-group-coverage-test");
            CoverageMapView? view = null;
            try
            {
                var host = go.AddComponent<DelegationBridgeHost>();
                yield return null; // Allow Awake() to construct Bridge.

                Assert.That(host.Bridge, Is.Not.Null);
                Assert.That(PlayModeSmokeOrbatSeeder.TrySeedWithEscortTaskGroup(host.Bridge), Is.True);
                host.BeginExecution();

                var world = new SmokeWorldStub(new[]
                {
                    PlayModeSmokeOrbatSeeder.FriendlyUnitId,
                    PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId,
                    PlayModeSmokeOrbatSeeder.HostileUnitId,
                });
                var wrappedSnapshot = PlayModeSmokeCoordinationFactsProvider.WrapSnapshot(world);

                world.SimTime = 1.0;
                host.RunTick(wrappedSnapshot, world);
                yield return null;

                Assert.That(host.LastCoordination.Groups, Has.Count.EqualTo(1));
                var group = host.LastCoordination.Groups[0];
                Assert.That(group.Coordination.GroupId, Is.EqualTo(PlayModeSmokeOrbatSeeder.TaskGroupId));
                Assert.That(group.Coordination.AssignedPackageId, Is.EqualTo(PlayModeSmokeCoordinationFactsProvider.PackageId));
                Assert.That(group.Effects.Count(e => e.Coverage.Status == CoverageStatus.Covered), Is.EqualTo(2));

                var canvas = new VisualElement { name = "smoke-coverage-canvas" };
                view = new CoverageMapView(canvas);
                view.Bind(host.LastCoordination, visible: true);

                Assert.That(view.PolygonCount, Is.EqualTo(2),
                    "CoverageMapView must bind both authored finite [0..1] smoke polygons.");
                Assert.That(view.StatusText, Does.Contain("Coverage: 2 polygons"));
                Assert.That(view.StatusText, Does.Not.Contain("INVALID"));
                Assert.That(view.StatusText, Does.Not.Contain("TRUNCATED"));

                // Exercise Group Hold through DelegationBridgeHost -> CoordinationCommandBridge.
                var holdFeedback = host.SubmitGroupDecision(PlayModeSmokeOrbatSeeder.TaskGroupId, CoordinationDecision.Hold);
                Assert.That(holdFeedback, Does.StartWith("Queued Hold:"));

                world.SimTime = 2.0;
                host.RunTick(wrappedSnapshot, world);
                yield return null;

                Assert.That(world.Applied.Count(a => a.Order.Kind == OrderKind.Hold), Is.EqualTo(2),
                    "Approved group Hold must expand to both live friendly escort members.");
            }
            finally
            {
                view?.Dispose();
                Object.Destroy(go);
            }
        }

        [Test]
        public void CoverageMapView_binds_detached_member_gap_polygon_cleanly_without_invalid_warnings()
        {
            var go = new GameObject("smoke-gap-host");
            try
            {
                var host = go.AddComponent<DelegationBridgeHost>();
                var bridge = host.Bridge;
                Assert.That(PlayModeSmokeOrbatSeeder.TrySeedWithEscortTaskGroup(bridge), Is.True);
                Assert.That(
                    bridge.TryTakeDirectControl(PlayModeSmokeCoordinationFactsProvider.FriendlyEscortEntityKey, simTime: 2.0),
                    Is.True);

                var world = new SmokeWorldStub(new[]
                {
                    PlayModeSmokeOrbatSeeder.FriendlyUnitId,
                    PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId,
                    PlayModeSmokeOrbatSeeder.HostileUnitId,
                })
                {
                    SimTime = 2.0,
                };

                var coordination = PlayModeSmokeCoordinationFactsProvider.BuildSnapshot(bridge, world);
                var group = coordination.Groups.Single();
                Assert.That(group.Effects.Any(e => e.Coverage.Status == CoverageStatus.Gap), Is.True);

                using var view = new CoverageMapView(new VisualElement { name = "smoke-gap-canvas" });
                view.Bind(coordination, visible: true);

                Assert.That(view.PolygonCount, Is.EqualTo(2));
                Assert.That(view.StatusText, Does.Not.Contain("INVALID"));
                Assert.That(view.StatusText, Does.Not.Contain("TRUNCATED"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
#endif
