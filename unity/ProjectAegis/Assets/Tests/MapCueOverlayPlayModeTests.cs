// Sprint 2 DRG-712 PlayMode verification: Companion MapCueOverlayPanelHost cues & zero-leak selection switch.
#if UNITY_5_3_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Unity.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Tests
{
    public sealed class MapCueOverlayPlayModeTests
    {
        private sealed class StubWorld : ISimWorldSnapshot, IOrderSink
        {
            public double SimTime { get; set; } = 1.0;
            public int ContactCount => 2;
            public int ActiveEngagementCount => 0;
            public TargetId? PrimaryHostileContactId => new("dl-hostile-1");
            public bool HasFireControlTrackOnPrimaryContact => true;
            public bool ObserverRadarEmconActive => true;
            public bool IsMemberAlive(TargetId memberId) => true;
            public void ApplyOrder(EntityKey entity, in Order order) { }
        }

        private readonly List<Object> _cleanup = new();

        [TearDown]
        public void TearDown()
        {
            for (var i = _cleanup.Count - 1; i >= 0; i--)
            {
                if (_cleanup[i] != null)
                {
                    Object.DestroyImmediate(_cleanup[i]);
                }
            }

            _cleanup.Clear();
        }

        [UnityTest]
        public IEnumerator Companion_overlay_renders_stale_datalink_icons_and_queued_order_badge_with_ignore_picking()
        {
            var go = new GameObject("sprint2-map-cue-overlay-test");
            _cleanup.Add(go);

            var bridgeHost = go.AddComponent<DelegationBridgeHost>();
            var document = CreateDocument(go);
            yield return null;

            var u1 = bridgeHost.Bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
            u1.Target.Slot.SetActive(new HumanController());
            var u2 = bridgeHost.Bridge.Registry.RegisterUnit(new EntityKey(2), "u2");
            u2.Target.Slot.SetActive(new HumanController());

            var log = bridgeHost.Bridge.Orchestrator.DecisionLog;
            log.AppendContactChange(new ContactChangeRecord(0, 1.0, 1, "u1", "dl-hostile-1", "hostile-1", "Unknown", "Identified"));
            log.AppendContactChange(new ContactChangeRecord(0, 40.0, 40, "u1", "c-fresh", "hostile-2", "Unknown", "Identified"));
            log.AppendPlayerOrder(new PlayerOrderRecord(0, 35.0, 35, new TargetId("u1"), OrderKind.Hold, ExecuteSimTick: 35));
            log.AppendPlayerOrder(new PlayerOrderRecord(0, 35.0, 35, new TargetId("u2"), OrderKind.Move, ExecuteSimTick: 99));

            bridgeHost.BeginExecution();
            var world = new StubWorld { SimTime = 40.0 };
            bridgeHost.RunTick(world, world);

            var overlay = go.AddComponent<MapCueOverlayPanelHost>();
            overlay.enabled = false;
            BuildOverlayTree(document.rootVisualElement, out var root, out var canvas, out _, out _);
            SetPrivateField(overlay, "bridgeHost", bridgeHost);
            overlay.enabled = true;
            yield return null;

            Assert.GreaterOrEqual(document.sortingOrder, 1);
            Assert.Less(document.sortingOrder, 10);
            AssertAllIgnorePicking(root);

            var dlElement = canvas.Q<VisualElement>(MapCueOverlayProjection.SymbolElementName("dl-hostile-1"));
            Assert.IsNotNull(dlElement);
            Assert.IsTrue(dlElement!.ClassListContains(MapCueOverlayProjection.StaleCssClass));
            Assert.IsTrue(dlElement.ClassListContains(MapCueOverlayProjection.DatalinkCssClass));

            var u1Element = canvas.Q<VisualElement>(MapCueOverlayProjection.SymbolElementName("u1"));
            Assert.IsNotNull(u1Element);
            Assert.IsTrue(u1Element!.ClassListContains(MapCueOverlayProjection.QueuedCssClass));

            var u2Element = canvas.Q<VisualElement>(MapCueOverlayProjection.SymbolElementName("u2"));
            if (u2Element != null)
            {
                Assert.IsFalse(u2Element.ClassListContains(MapCueOverlayProjection.QueuedCssClass));
            }
        }

        [UnityTest]
        public IEnumerator Selection_switch_clears_previous_contact_and_unit_badges_in_play_mode()
        {
            var go = new GameObject("sprint2-map-cue-selection-leak-test");
            _cleanup.Add(go);

            var bridgeHost = go.AddComponent<DelegationBridgeHost>();
            var document = CreateDocument(go);
            yield return null;

            var u1 = bridgeHost.Bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
            u1.Target.Slot.SetActive(new HumanController());
            var u2 = bridgeHost.Bridge.Registry.RegisterUnit(new EntityKey(2), "u2");
            u2.Target.Slot.SetActive(new HumanController());

            var log = bridgeHost.Bridge.Orchestrator.DecisionLog;
            log.AppendContactChange(new ContactChangeRecord(0, 1.0, 1, "u1", "dl-hostile-1", "hostile-1", "Unknown", "Suspect"));
            log.AppendContactChange(new ContactChangeRecord(0, 40.0, 40, "u1", "c-fresh", "hostile-2", "Unknown", "Identified"));
            log.AppendPlayerOrder(new PlayerOrderRecord(0, 35.0, 35, new TargetId("u1"), OrderKind.Hold, ExecuteSimTick: 35));

            bridgeHost.BeginExecution();
            var world = new StubWorld { SimTime = 40.0 };
            bridgeHost.RunTick(world, world);

            var overlay = go.AddComponent<MapCueOverlayPanelHost>();
            overlay.enabled = false;
            BuildOverlayTree(document.rootVisualElement, out _, out _, out var contactBadge, out var unitBadge);
            SetPrivateField(overlay, "bridgeHost", bridgeHost);
            overlay.enabled = true;

            bridgeHost.SelectContact("dl-hostile-1");
            yield return null;
            Assert.AreEqual(DisplayStyle.Flex, contactBadge.style.display.value);
            StringAssert.Contains(MapCueOverlayProjection.StaleTokenText, contactBadge.text);
            StringAssert.Contains(MapCueOverlayProjection.DatalinkTokenText, contactBadge.text);

            // Switch to fresh contact -> previous stale/datalink cue must vanish completely.
            bridgeHost.SelectContact("c-fresh");
            yield return null;
            Assert.AreEqual(DisplayStyle.None, contactBadge.style.display.value);
            Assert.AreEqual(string.Empty, contactBadge.text);
            Assert.IsFalse(contactBadge.ClassListContains(MapCueOverlayProjection.StaleCssClass));
            Assert.IsFalse(contactBadge.ClassListContains(MapCueOverlayProjection.DatalinkCssClass));

            // Select queued unit u1 -> unit badge visible; switch to idle u2 -> cleared.
            bridgeHost.SelectUnit("u1");
            yield return null;
            Assert.AreEqual(DisplayStyle.Flex, unitBadge.style.display.value);
            StringAssert.Contains(MapCueOverlayProjection.QueuedBadgeText, unitBadge.text);

            bridgeHost.SelectUnit("u2");
            yield return null;
            Assert.AreEqual(DisplayStyle.None, unitBadge.style.display.value);
            Assert.AreEqual(string.Empty, unitBadge.text);
            Assert.IsFalse(unitBadge.ClassListContains(MapCueOverlayProjection.QueuedCssClass));
        }

        private UIDocument CreateDocument(GameObject host)
        {
            var document = host.AddComponent<UIDocument>();
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            var theme = ScriptableObject.CreateInstance<ThemeStyleSheet>();
            _cleanup.Add(settings);
            _cleanup.Add(theme);
            settings.themeStyleSheet = theme;
            document.panelSettings = settings;
            return document;
        }

        private static void BuildOverlayTree(
            VisualElement documentRoot,
            out VisualElement root,
            out VisualElement canvas,
            out Label contactBadge,
            out Label unitBadge)
        {
            root = new VisualElement { name = MapCueOverlayProjection.RootElementName, pickingMode = PickingMode.Ignore };
            var strip = new VisualElement { name = MapCueOverlayProjection.SelectedStripElementName, pickingMode = PickingMode.Ignore };
            contactBadge = new Label { name = MapCueOverlayProjection.SelectedContactBadgeElementName, pickingMode = PickingMode.Ignore };
            var contactFixture = new Label { name = MapCueOverlayProjection.SelectedContactFixtureElementName, pickingMode = PickingMode.Ignore };
            unitBadge = new Label { name = MapCueOverlayProjection.SelectedUnitOrderBadgeElementName, pickingMode = PickingMode.Ignore };
            strip.Add(contactBadge);
            strip.Add(contactFixture);
            strip.Add(unitBadge);
            canvas = new VisualElement { name = MapCueOverlayProjection.CanvasElementName, pickingMode = PickingMode.Ignore };
            root.Add(strip);
            root.Add(canvas);
            documentRoot.Add(root);
        }

        private static void AssertAllIgnorePicking(VisualElement element)
        {
            Assert.AreEqual(PickingMode.Ignore, element.pickingMode, $"Element {element.name} must ignore picking.");
            foreach (var child in element.Children())
            {
                AssertAllIgnorePicking(child);
            }
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing private field {name}");
            field!.SetValue(target, value);
        }
    }
}
#endif
