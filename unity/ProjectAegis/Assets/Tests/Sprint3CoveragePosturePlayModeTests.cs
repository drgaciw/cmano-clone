#if UNITY_5_3_OR_NEWER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectAegis.Delegation.AfterAction;
using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;
using ProjectAegis.Unity.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Tests
{
    public sealed class Sprint3CoveragePosturePlayModeTests
    {
        private sealed class StubWorld : ISimWorldSnapshot, IOrderSink
        {
            private readonly Dictionary<string, bool> _alive;

            public StubWorld(Dictionary<string, bool> alive) => _alive = alive;

            public double SimTime { get; set; } = 1.0;
            public int ContactCount => 1;
            public int ActiveEngagementCount => 0;
            public TargetId? PrimaryHostileContactId => new TargetId(PlayModeSmokeOrbatSeeder.HostileUnitId);
            public bool HasFireControlTrackOnPrimaryContact => true;
            public bool ObserverRadarEmconActive => true;
            public bool IsMemberAlive(TargetId memberId) => _alive.TryGetValue(memberId.Value, out var ok) && ok;
            public void ApplyOrder(EntityKey entity, in Order order) { }
        }

        private GameObject? _go;
        private PanelSettings? _settings;
        private ThemeStyleSheet? _theme;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
            if (_settings != null) UnityEngine.Object.DestroyImmediate(_settings);
            if (_theme != null) UnityEngine.Object.DestroyImmediate(_theme);
            _go = null;
            _settings = null;
            _theme = null;
        }

        private UIDocument CreateDocument(GameObject host)
        {
            var document = host.AddComponent<UIDocument>();
            _settings = ScriptableObject.CreateInstance<PanelSettings>();
            _theme = ScriptableObject.CreateInstance<ThemeStyleSheet>();
            _settings.themeStyleSheet = _theme;
            document.panelSettings = _settings;
            return document;
        }

        [UnityTest]
        public IEnumerator CoverageMapView_respects_tactical_operational_strategic_scale_tiers_without_breaking_legacy_bind()
        {
            _go = new GameObject("sprint3-coverage-scale-test");
            var bridgeHost = _go.AddComponent<DelegationBridgeHost>();
            var document = CreateDocument(_go);
            yield return null;

            Assert.IsTrue(PlayModeSmokeOrbatSeeder.TrySeedWithEscortTaskGroup(bridgeHost.Bridge));
            bridgeHost.BeginExecution();
            var world = new StubWorld(new Dictionary<string, bool>
            {
                [PlayModeSmokeOrbatSeeder.FriendlyUnitId] = true,
                [PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId] = true,
                [PlayModeSmokeOrbatSeeder.HostileUnitId] = true,
            });
            var wrapped = PlayModeSmokeCoordinationFactsProvider.WrapSnapshot(world);
            bridgeHost.RunTick(wrapped, world);
            yield return null;

            var canvas = new VisualElement { name = "scale-coverage-canvas" };
            canvas.style.width = 400;
            canvas.style.height = 400;
            document.rootVisualElement.Add(canvas);
            yield return null;

            using var view = new CoverageMapView(canvas);

            // 1. Legacy 2-arg Bind remains untouched and shows polygons + labels
            view.Bind(bridgeHost.LastCoordination, true);
            Assert.AreEqual(2, view.PolygonCount);
            Assert.AreEqual(CoverageMapScaleTier.Unknown, view.LastScaleDecision.Tier);

            var posture = CoveragePostureProjection.Project(bridgeHost.LastCoordination);

            // 2. Tactical scale (20 NM): 2 polygons + labels Flex
            view.Bind(bridgeHost.LastCoordination, true, new MapScaleState("20 NM", "ALT 40 km", 20, 40_000), posture);
            Assert.AreEqual(2, view.PolygonCount);
            Assert.AreEqual(CoverageMapScaleTier.Tactical, view.LastScaleDecision.Tier);
            Assert.IsTrue(view.LastScaleDecision.ShowSectorLabels);
            StringAssert.Contains("TACTICAL", view.StatusText);

            // 3. Operational scale (120 NM): 2 polygons + labels suppressed
            view.Bind(bridgeHost.LastCoordination, true, new MapScaleState("120 NM", "ALT 250 km", 120, 250_000), posture);
            Assert.AreEqual(2, view.PolygonCount);
            Assert.AreEqual(CoverageMapScaleTier.Operational, view.LastScaleDecision.Tier);
            Assert.IsFalse(view.LastScaleDecision.ShowSectorLabels);
            var labels = canvas.Query<Label>("coverage-label").ToList();
            Assert.IsTrue(labels.All(l => l.style.display == DisplayStyle.None));

            // 4. Strategic scale (400 NM): polygons suppressed, posture summary shown
            view.Bind(bridgeHost.LastCoordination, true, new MapScaleState("400 NM", "ALT 1200 km", 400, 1_200_000), posture);
            Assert.AreEqual(0, view.PolygonCount);
            Assert.AreEqual(CoverageMapScaleTier.Strategic, view.LastScaleDecision.Tier);
            StringAssert.Contains("STRATEGIC", view.StatusText);
            StringAssert.Contains("2 covered", view.StatusText);
        }

        [UnityTest]
        public IEnumerator CoveragePostureOverlayPanelHost_renders_layered_posture_diff_on_detachment_and_hides_on_history_inspection()
        {
            _go = new GameObject("sprint3-posture-overlay-test");
            var bridgeHost = _go.AddComponent<DelegationBridgeHost>();
            var document = CreateDocument(_go);
            yield return null;

            Assert.IsTrue(PlayModeSmokeOrbatSeeder.TrySeedWithEscortTaskGroup(bridgeHost.Bridge));
            bridgeHost.BeginExecution();
            var world = new StubWorld(new Dictionary<string, bool>
            {
                [PlayModeSmokeOrbatSeeder.FriendlyUnitId] = true,
                [PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId] = true,
                [PlayModeSmokeOrbatSeeder.HostileUnitId] = true,
            });
            var wrapped = PlayModeSmokeCoordinationFactsProvider.WrapSnapshot(world);
            bridgeHost.RunTick(wrapped, world);
            yield return null;

            var overlayHost = _go.AddComponent<CoveragePostureOverlayPanelHost>();
            overlayHost.enabled = false;
            BuildOverlayTree(
                document.rootVisualElement,
                out var overlayRoot,
                out var summaryLabel,
                out var diffHeadline,
                out var diffDetail);
            SetPrivateField(overlayHost, "bridgeHost", bridgeHost);
            overlayHost.enabled = true;
            yield return null;

            Assert.AreEqual(CoveragePostureOverlayPanelHost.DefaultOverlaySortingOrder, document.sortingOrder);
            Assert.IsTrue(AllIgnorePicking(overlayRoot));
            Assert.AreEqual(2, overlayHost.LastPosture.TotalCovered);
            StringAssert.Contains("2 covered", summaryLabel.text);

            // Detach u2 -> posture degrades NONE -> SPLIT
            overlayHost.CaptureBaseline();
            Assert.IsTrue(bridgeHost.Bridge.TryTakeDirectControl(
                PlayModeSmokeCoordinationFactsProvider.FriendlyEscortEntityKey, simTime: 2.0));
            world.SimTime = 2.0;
            bridgeHost.RunTick(wrapped, world);
            yield return null;

            Assert.AreEqual(CoveragePostureDeltaKind.Degraded, overlayHost.LastDiff.OverallDelta);
            StringAssert.Contains("SPLIT", diffHeadline.text);
            StringAssert.Contains(PlayModeSmokeCoordinationFactsProvider.EscortCoverageId, diffDetail.text);

            // What-if hypothetical C2 loss (non-mutating)
            overlayHost.SimulateElementChange(
                PlayModeSmokeOrbatSeeder.TaskGroupId,
                PlayModeSmokeOrbatSeeder.FriendlyUnitId,
                HypotheticalElementChangeKind.Loss);
            yield return null;
            Assert.IsTrue(overlayHost.LastDiff.IsHypothetical);
            Assert.AreEqual(CoveragePostureDeltaKind.Collapsed, overlayHost.LastDiff.OverallDelta);
            StringAssert.Contains("[WHAT-IF]", diffHeadline.text);
            StringAssert.Contains("COLLAPSED", diffHeadline.text);
            StringAssert.Contains("u1 (C2): Available->Lost", diffHeadline.text);

            // History inspection hides passive overlay
            var timelineRows = new[]
            {
                new CombatEvent(
                    CombatEventPhase.Firing,
                    PlayModeSmokeOrbatSeeder.FriendlyUnitId,
                    PlayModeSmokeOrbatSeeder.HostileUnitId,
                    "Gun",
                    "Launch",
                    1UL,
                    2.0,
                    2UL,
                    "Timeline event"),
            };
            bridgeHost.CommandTimeline.Capture(
                CombatPresentationFrame.Empty with
                {
                    SimTime = 2.0,
                    Events = new CombatEventSnapshot(timelineRows),
                },
                Array.Empty<MapSymbolEntry>());
            var entry = bridgeHost.CommandTimeline.Filter(new AfterActionLedgerFilter())[0];
            Assert.IsTrue(bridgeHost.InspectTimelineEvent(entry));
            yield return null;

            Assert.AreEqual(DisplayStyle.None, overlayRoot.style.display.value);
        }

        private static bool AllIgnorePicking(VisualElement root)
        {
            if (root.pickingMode != PickingMode.Ignore)
            {
                return false;
            }

            foreach (var child in root.Children())
            {
                if (!AllIgnorePicking(child))
                {
                    return false;
                }
            }

            return true;
        }

        private static void BuildOverlayTree(
            VisualElement docRoot,
            out VisualElement overlayRoot,
            out Label summaryLabel,
            out Label diffHeadline,
            out Label diffDetail)
        {
            overlayRoot = new VisualElement { name = "coverage-posture-overlay-root", pickingMode = PickingMode.Ignore };
            var card = new VisualElement { name = "coverage-posture-card", pickingMode = PickingMode.Ignore };
            var strip = new VisualElement { name = "coverage-posture-header-strip", pickingMode = PickingMode.Ignore };
            strip.Add(new Label { name = "coverage-posture-scale-badge", pickingMode = PickingMode.Ignore });
            strip.Add(new Label { name = "coverage-posture-authority-badge", pickingMode = PickingMode.Ignore });
            card.Add(strip);

            summaryLabel = new Label { name = "coverage-posture-summary-label", pickingMode = PickingMode.Ignore };
            card.Add(summaryLabel);
            card.Add(new Label { name = "coverage-posture-layers-label", pickingMode = PickingMode.Ignore });
            card.Add(new Label { name = "coverage-posture-gaps-label", pickingMode = PickingMode.Ignore });

            var diffBox = new VisualElement { name = "coverage-posture-diff-box", pickingMode = PickingMode.Ignore };
            diffHeadline = new Label { name = "coverage-posture-diff-headline", pickingMode = PickingMode.Ignore };
            diffDetail = new Label { name = "coverage-posture-diff-detail", pickingMode = PickingMode.Ignore };
            diffBox.Add(diffHeadline);
            diffBox.Add(diffDetail);
            card.Add(diffBox);

            overlayRoot.Add(card);
            docRoot.Add(overlayRoot);
        }

        private static void SetPrivateField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
    }
}
#endif
