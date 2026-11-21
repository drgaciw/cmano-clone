// UAT D25 / C8 evidence: the unit-order toolbar must confirm a successful order with
// "ISSUED: <command> → <unit>" (previously the success branch cleared the status line).
// Exercises the real UnitOrderToolbarHost click wiring against a real DelegationBridgeHost.
#if UNITY_5_3_OR_NEWER
using System.Collections;
using System.Reflection;
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
    public sealed class UnitOrderToolbarIssuedStatusPlayModeTests
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

        private GameObject? _go;
        private PanelSettings? _settings;
        private ThemeStyleSheet? _theme;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            if (_settings != null) Object.DestroyImmediate(_settings);
            if (_theme != null) Object.DestroyImmediate(_theme);
            _go = null;
            _settings = null;
            _theme = null;
        }

        private IEnumerator BuildToolbar(bool seedTick, System.Action<DelegationBridgeHost, Label, Button> onReady)
        {
            _go = new GameObject("unit-order-toolbar-issued-status");
            var bridge = _go.AddComponent<DelegationBridgeHost>();
            var document = _go.AddComponent<UIDocument>();
            _settings = ScriptableObject.CreateInstance<PanelSettings>();
            _theme = ScriptableObject.CreateInstance<ThemeStyleSheet>();
            _settings.themeStyleSheet = _theme;
            document.panelSettings = _settings;
            Assert.IsTrue(SimplePlayModeSimHost.TrySeedSmokeOrbat(bridge.Bridge));
            bridge.BeginExecution();
            if (seedTick)
            {
                var world = new StubWorld();
                bridge.RunTick(world, world);
            }

            yield return null;

            var toolbar = _go.AddComponent<UnitOrderToolbarHost>();
            toolbar.enabled = false;
            var status = BuildToolbarTree(document.rootVisualElement, out var holdButton);
            SetPrivateField(toolbar, "bridgeHost", bridge);
            toolbar.enabled = true;
            yield return null;

            onReady(bridge, status, holdButton);
        }

        [UnityTest]
        public IEnumerator Hold_click_with_selected_unit_shows_issued_status()
        {
            DelegationBridgeHost? bridge = null;
            Label? status = null;
            Button? hold = null;
            yield return BuildToolbar(true, (b, s, h) => { bridge = b; status = s; hold = h; });

            bridge!.SelectUnit(SimplePlayModeSimHost.SmokeFriendlyUnitId);
            Assert.AreEqual(SimplePlayModeSimHost.SmokeFriendlyUnitId, bridge.SelectedUnitId);
            yield return null; // LateUpdate -> Refresh enables the buttons for the selection.
            Assert.IsTrue(hold!.enabledSelf, "Hold must be enabled once a unit is selected.");

            using (var submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = hold;
                hold.SendEvent(submit);
            }

            yield return null;
            StringAssert.Contains("ISSUED: hold → u1", status!.text);
        }

        [UnityTest]
        public IEnumerator Toolbar_without_selection_shows_no_selection_status()
        {
            DelegationBridgeHost? bridge = null;
            Label? status = null;
            Button? hold = null;
            yield return BuildToolbar(false, (b, s, h) => { bridge = b; status = s; hold = h; });

            yield return null;
            Assert.IsTrue(string.IsNullOrEmpty(bridge!.SelectedUnitId),
                "Fixture must not have an implicit selection.");
            Assert.IsFalse(hold!.enabledSelf, "Hold must be disabled with no selection.");
            StringAssert.Contains("NO_SELECTION", status!.text);
        }

        private static Label BuildToolbarTree(VisualElement documentRoot, out Button holdButton)
        {
            var root = new VisualElement { name = "unit-order-toolbar-root" };
            holdButton = new Button { name = "order-hold" };
            root.Add(holdButton);
            root.Add(new Button { name = "order-rtb" });
            root.Add(new Button { name = "order-plot-course" });
            root.Add(new Button { name = "order-emcon" });
            root.Add(new Button { name = "order-sensors" });
            root.Add(new Button { name = "order-engage" });
            var status = new Label { name = "order-status-line" };
            root.Add(status);
            documentRoot.Add(root);
            return status;
        }

        private static void SetPrivateField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
    }
}
#endif
