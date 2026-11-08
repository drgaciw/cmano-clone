// EditMode tests for MapSymbolPool (fix #3, unity-integration-review): prove the map symbol
// renderer reuses VisualElements across syncs instead of clearing + recreating the canvas.
#if UNITY_5_3_OR_NEWER
using System.Collections;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Unity.Runtime;

namespace ProjectAegis.Unity.Tests
{
    public sealed class MapSymbolPoolTests
    {
        private static MapSymbolDisplayRow Row(string id, float x = 0.5f) =>
            new(id, "■", id.ToUpperInvariant(), x, 0.5f, "friendly", false);

        private static List<MapSymbolDisplayRow> Rows(params MapSymbolDisplayRow[] rows) => rows.ToList();

        [Test]
        public void Sync_reuses_the_same_elements_on_identical_input()
        {
            var canvas = new VisualElement();
            var pool = new MapSymbolPool(canvas);

            pool.Sync(Rows(Row("a"), Row("b"), Row("c")), _ => { });
            Assert.AreEqual(3, canvas.childCount);
            var firstPass = canvas.Children().ToList();

            pool.Sync(Rows(Row("a"), Row("b"), Row("c")), _ => { });
            var secondPass = canvas.Children().ToList();

            Assert.AreEqual(3, canvas.childCount, "No net element change expected.");
            CollectionAssert.AreEquivalent(firstPass, secondPass,
                "Identical input should reuse the same VisualElement instances (no clear+recreate).");
        }

        [Test]
        public void Sync_adds_new_ids_and_removes_absent_ids()
        {
            var canvas = new VisualElement();
            var pool = new MapSymbolPool(canvas);

            pool.Sync(Rows(Row("a"), Row("b")), _ => { });
            Assert.AreEqual(2, pool.Count);

            pool.Sync(Rows(Row("a"), Row("c"), Row("d")), _ => { });
            Assert.AreEqual(3, pool.Count);
            Assert.AreEqual(3, canvas.childCount);

            var ids = canvas.Children().Select(e => (string)e.userData).ToList();
            CollectionAssert.AreEquivalent(new[] { "a", "c", "d" }, ids);
        }

        [Test]
        public void Sync_updates_position_of_a_reused_element_in_place()
        {
            var canvas = new VisualElement();
            var pool = new MapSymbolPool(canvas);

            pool.Sync(Rows(Row("a", 0.25f)), _ => { });
            var element = canvas.ElementAt(0);

            pool.Sync(Rows(Row("a", 0.75f)), _ => { });

            Assert.AreSame(element, canvas.ElementAt(0), "Same id must reuse the element, not recreate it.");
            Assert.AreEqual(75f, element.style.left.value.value, 0.01f, "Position should update in place.");
        }
    }

    public sealed class RightUnitPanelCommandFeedbackTests
    {
        private sealed class StubWorld : ISimWorldSnapshot, IOrderSink
        {
            public double SimTime { get; set; } = 1;
            public int ContactCount => 1;
            public int ActiveEngagementCount => 0;
            public TargetId? PrimaryHostileContactId => new(SimplePlayModeSimHost.SmokeHostileUnitId);
            public bool HasFireControlTrackOnPrimaryContact => true;
            public bool ObserverRadarEmconActive => true;
            public bool IsMemberAlive(TargetId memberId) => true;
            public void ApplyOrder(EntityKey entity, in Order order) { }
        }

        [UnityTest]
        public IEnumerator Click_reports_queued_and_refresh_preserves_feedback_until_selection_changes()
        {
            var go = new GameObject("right-unit-feedback");
            PanelSettings settings = null;
            ThemeStyleSheet theme = null;
            try
            {
                var bridge = go.AddComponent<DelegationBridgeHost>();
                var document = go.AddComponent<UIDocument>();
                settings = ScriptableObject.CreateInstance<PanelSettings>();
                theme = ScriptableObject.CreateInstance<ThemeStyleSheet>();
                settings.themeStyleSheet = theme;
                document.panelSettings = settings;
                Assert.IsTrue(SimplePlayModeSimHost.TrySeedSmokeOrbat(bridge.Bridge));
                bridge.BeginExecution();
                var world = new StubWorld();
                bridge.RunTick(world, world);

                yield return null;
                var panel = go.AddComponent<RightUnitPanelHost>();
                panel.enabled = false;
                var attackLine = BuildPanelTree(document.rootVisualElement, out var fireSingleButton);
                SetPrivateField(panel, "bridgeHost", bridge);
                panel.enabled = true;
                yield return null;
                Assert.IsTrue(GetPrivateField<bool>(panel, "_attackHandlersRegistered"),
                    "OnEnable should register handlers against the ready visual tree.");
                Assert.AreSame(attackLine, GetPrivateField<Label>(panel, "_attackOptionsLine"));

                using (var submit = NavigationSubmitEvent.GetPooled())
                {
                    submit.target = fireSingleButton;
                    fireSingleButton.SendEvent(submit);
                }
                StringAssert.Contains("QUEUED: Fire 1 round", attackLine.text);

                InvokePrivate(panel, "Refresh");
                StringAssert.Contains("QUEUED: Fire 1 round", attackLine.text,
                    "The per-frame refresh must not erase command acknowledgement.");

                var originalSelection = bridge.SelectedUnitId;
                Assert.IsNotNull(originalSelection, "A queued command must have a selected unit.");
                var nextSelection = originalSelection == SimplePlayModeSimHost.SmokeFriendlyUnitId
                    ? SimplePlayModeSimHost.SmokeHostileUnitId
                    : SimplePlayModeSimHost.SmokeFriendlyUnitId;
                bridge.SelectUnit(nextSelection);
                Assert.AreEqual(nextSelection, bridge.SelectedUnitId,
                    "The fixture must prove selection changed before checking feedback reset.");
                InvokePrivate(panel, "Refresh");
                StringAssert.DoesNotContain("QUEUED:", attackLine.text,
                    "Feedback for one unit must not be shown after selection changes.");
            }
            finally
            {
                Object.DestroyImmediate(go);
                if (settings != null) Object.DestroyImmediate(settings);
                if (theme != null) Object.DestroyImmediate(theme);
            }
        }

        [UnityTest]
        public IEnumerator Click_reports_facade_denial_reason()
        {
            var go = new GameObject("right-unit-denial");
            PanelSettings settings = null;
            ThemeStyleSheet theme = null;
            try
            {
                var bridge = go.AddComponent<DelegationBridgeHost>();
                var document = go.AddComponent<UIDocument>();
                settings = ScriptableObject.CreateInstance<PanelSettings>();
                theme = ScriptableObject.CreateInstance<ThemeStyleSheet>();
                settings.themeStyleSheet = theme;
                document.panelSettings = settings;
                Assert.IsTrue(SimplePlayModeSimHost.TrySeedSmokeOrbat(bridge.Bridge));
                bridge.BeginExecution();
                var world = new StubWorld();
                bridge.RunTick(world, world);

                yield return null;
                var panel = go.AddComponent<RightUnitPanelHost>();
                panel.enabled = false;
                var attackLine = BuildPanelTree(document.rootVisualElement, out _);
                SetPrivateField(panel, "bridgeHost", bridge);
                panel.enabled = true;
                yield return null;
                Assert.IsTrue(GetPrivateField<bool>(panel, "_attackHandlersRegistered"),
                    "OnEnable should register handlers against the ready visual tree.");
                Assert.AreSame(attackLine, GetPrivateField<Label>(panel, "_attackOptionsLine"));

                InvokePrivate(panel, "OnAttackOptionClicked", "unknown-option");
                StringAssert.Contains("DENIED: Unknown command (UNKNOWN_OPTION)", attackLine.text);
            }
            finally
            {
                Object.DestroyImmediate(go);
                if (settings != null) Object.DestroyImmediate(settings);
                if (theme != null) Object.DestroyImmediate(theme);
            }
        }

        private static Label BuildPanelTree(VisualElement documentRoot, out Button fireSingleButton)
        {
            var root = new VisualElement { name = "unit-detail-root" };
            root.Add(new Label { name = "unit-id-line" });
            root.Add(new Label { name = "status-line" });
            root.Add(new Label { name = "magazine-line" });
            root.Add(new Label { name = "emcon-line" });
            root.Add(new Label { name = "doctrine-line" });
            var attackLine = new Label { name = "attack-options-line" };
            root.Add(attackLine);
            fireSingleButton = new Button { name = "attack-fire-single" };
            root.Add(fireSingleButton);
            root.Add(new Button { name = "attack-fire-salvo" });
            root.Add(new Button { name = "attack-hold-fire" });
            documentRoot.Add(root);
            return attackLine;
        }

        private static void SetPrivateField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);

        private static T GetPrivateField<T>(object target, string name) =>
            (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target);

        private static void InvokePrivate(object target, string name, params object[] args) =>
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args);
    }
}
#endif
