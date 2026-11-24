#if UNITY_5_3_OR_NEWER
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Unity.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Tests
{
    public sealed class Sprint3JevRoeGatePlayModeTests
    {
        private sealed class StubWorld : ISimWorldSnapshot, IOrderSink
        {
            public double SimTime { get; set; } = 3.0;
            public int ContactCount => 1;
            public int ActiveEngagementCount => 0;
            public TargetId? PrimaryHostileContactId => new TargetId(SimplePlayModeSimHost.SmokeHostileUnitId);
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
        public IEnumerator PendingApprovalPanel_gates_jev_suggested_order_on_roe_noul_and_routes_accept_via_coordination_bridge()
        {
            _go = new GameObject("sprint3-jev-roe-gate-test");
            var bridgeHost = _go.AddComponent<DelegationBridgeHost>();
            var document = CreateDocument(_go);
            yield return null;

            Assert.IsTrue(PlayModeSmokeOrbatSeeder.TrySeedWithEscortTaskGroup(bridgeHost.Bridge));
            bridgeHost.BeginExecution();
            var world = new StubWorld { SimTime = 3.0 };
            bridgeHost.RunTick(world, world);
            yield return null;

            var panelHost = _go.AddComponent<PendingApprovalPanelHost>();
            panelHost.enabled = false;
            BuildPendingApprovalTree(
                document.rootVisualElement,
                out var jevRoot,
                out var jevAccept,
                out var jevDismiss);
            SetPrivateField(panelHost, "bridgeHost", bridgeHost);
            panelHost.enabled = true;
            yield return null;

            // 1. Offline / low Noul -> fails closed
            bridgeHost.EvaluateJevSuggestedOrder(
                "{\"schema\":\"aegis.jev.vs3.state.v0\",\"skillAvailability\":{\"jevDecide\":false},\"proposal\":{\"groupId\":\"tg-baltic-escort\",\"decision\":\"Withdraw\"},\"jevDecision\":{\"model\":\"typesafe/jev-1.13\",\"suggest_ok_under_roe\":\"yes\",\"roeNoul\":0.92,\"order_proposal_risk\":\"low\"}}");
            yield return null;
            Assert.AreEqual(DisplayStyle.None, jevRoot.resolvedStyle.display);
            Assert.IsFalse(jevAccept.enabledSelf);
            Assert.AreEqual(0, bridgeHost.Bridge.Orchestrator.DecisionLog.PlayerOrders.Count);

            // 2. Valid ROE Noul >= 0.80 -> visible, zero auto-fire before click
            bridgeHost.EvaluateJevSuggestedOrder(
                "{\"schema\":\"aegis.jev.vs3.state.v0\",\"skillAvailability\":{\"jevDecide\":true},\"proposal\":{\"groupId\":\"tg-baltic-escort\",\"decision\":\"Withdraw\"},\"jevDecision\":{\"model\":\"typesafe/jev-1.13\",\"suggest_ok_under_roe\":\"yes\",\"roeNoul\":0.88,\"order_proposal_risk\":\"low\"}}");
            yield return null;
            Assert.AreEqual(DisplayStyle.Flex, jevRoot.style.display.value);
            Assert.IsTrue(jevAccept.enabledSelf);
            Assert.AreEqual(0, bridgeHost.Bridge.Orchestrator.DecisionLog.PlayerOrders.Count);

            // 3. Dismiss leaves zero mutation
            SendSubmit(jevDismiss);
            yield return null;
            Assert.AreEqual(DisplayStyle.None, jevRoot.style.display.value);
            Assert.AreEqual(0, bridgeHost.Bridge.Orchestrator.DecisionLog.PlayerOrders.Count);

            // 4. Re-evaluate and click Accept -> enqueues through CoordinationCommandBridge
            bridgeHost.EvaluateJevSuggestedOrder(
                "{\"schema\":\"aegis.jev.vs3.state.v0\",\"skillAvailability\":{\"jevDecide\":true},\"proposal\":{\"groupId\":\"tg-baltic-escort\",\"decision\":\"Withdraw\"},\"jevDecision\":{\"model\":\"typesafe/jev-1.13\",\"suggest_ok_under_roe\":\"yes\",\"roeNoul\":0.88,\"order_proposal_risk\":\"low\"}}");
            yield return null;
            SendSubmit(jevAccept);
            yield return null;
            Assert.AreEqual(1, bridgeHost.Bridge.Orchestrator.DecisionLog.PlayerOrders.Count);
            Assert.AreEqual("tg-baltic-escort", bridgeHost.Bridge.Orchestrator.DecisionLog.PlayerOrders[0].UnitId.Value);
        }

        private static void BuildPendingApprovalTree(
            VisualElement root,
            out VisualElement jevRoot,
            out Button jevAccept,
            out Button jevDismiss)
        {
            var container = new VisualElement { name = "pending-approval-root" };
            container.Add(new Label { name = "pending-approval-header" });
            container.Add(new Label { name = "pending-approval-badge" });
            container.Add(new Label { name = "pending-approval-empty" });
            container.Add(new ListView { name = "pending-approval-list" });
            container.Add(new Label { name = "pending-approval-authority" });
            container.Add(new Button { name = "pending-approval-approve" });
            container.Add(new Button { name = "pending-approval-reject" });

            var coordConfirm = new VisualElement { name = "pending-coordination-confirm" };
            coordConfirm.Add(new Label { name = "pending-coordination-confirm-title" });
            coordConfirm.Add(new Label { name = "pending-coordination-confirm-body" });
            coordConfirm.Add(new Button { name = "pending-coordination-confirm-accept" });
            coordConfirm.Add(new Button { name = "pending-coordination-confirm-cancel" });
            container.Add(coordConfirm);

            jevRoot = new VisualElement { name = "pending-jev-suggested-order" };
            jevRoot.Add(new Label { name = "pending-jev-suggested-order-title" });
            jevRoot.Add(new Label { name = "pending-jev-suggested-order-body" });
            jevAccept = new Button { name = "pending-jev-suggested-order-accept", text = "ACCEPT" };
            jevDismiss = new Button { name = "pending-jev-suggested-order-dismiss", text = "DISMISS" };
            jevRoot.Add(jevAccept);
            jevRoot.Add(jevDismiss);
            container.Add(jevRoot);

            container.Add(new Label { name = "pending-approval-status" });
            root.Add(container);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing private field {fieldName}");
            field!.SetValue(target, value);
        }

        private static void SendSubmit(Button button)
        {
            using (var evt = NavigationSubmitEvent.GetPooled())
            {
                evt.target = button;
                button.SendEvent(evt);
            }
        }
    }
}
#endif
