#if UNITY_5_3_OR_NEWER
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Input;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Skills;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;
using ProjectAegis.Unity.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Tests
{
    public sealed class Sprint3SupervisedAdvicePlayModeTests
    {
        private sealed class StubWorld : ISimWorldSnapshot, IOrderSink
        {
            public double SimTime { get; set; } = 5.0;
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
        public IEnumerator SupervisedAdvicePanel_renders_evidence_and_routes_accept_dismiss_and_full_auto_opt_in()
        {
            _go = new GameObject("sprint3-supervised-advice-test");
            var bridgeHost = _go.AddComponent<DelegationBridgeHost>();
            var document = CreateDocument(_go);
            yield return null;

            Assert.IsTrue(SimplePlayModeSimHost.TrySeedSmokeOrbat(bridgeHost.Bridge));
            DoctrineOverrideCommand.TryApply(bridgeHost.Bridge.Orchestrator, new TargetId("u1"), "WeaponsFree", 0.0);
            bridgeHost.BeginExecution();
            var world = new StubWorld { SimTime = 5.0 };
            bridgeHost.RunTick(world, world);
            bridgeHost.SelectUnit("u1");
            bridgeHost.SelectContact("c1");
            yield return null;

            var panelHost = _go.AddComponent<SupervisedAdvicePanelHost>();
            panelHost.enabled = false;
            BuildSupervisedAdviceTree(
                document.rootVisualElement,
                out var headerLabel,
                out var evidenceList,
                out var optInToggle,
                out var acceptButton,
                out var dismissButton,
                out var statusLabel);
            SetPrivateField(panelHost, "bridgeHost", bridgeHost);
            panelHost.enabled = true;
            yield return null;

            Assert.AreEqual(18, document.sortingOrder);
            Assert.IsNotNull(headerLabel);
            Assert.GreaterOrEqual(evidenceList.itemsSource != null ? evidenceList.itemsSource.Count : 0, 1);

            var ordersBeforeDismiss = bridgeHost.Bridge.Orchestrator.DecisionLog.PlayerOrders.Count;
            var fpBeforeDismiss = bridgeHost.Bridge.Orchestrator.DecisionLog.ComputeFingerprint();
            SendSubmit(dismissButton);
            yield return null;
            Assert.AreEqual(ordersBeforeDismiss, bridgeHost.Bridge.Orchestrator.DecisionLog.PlayerOrders.Count);
            Assert.AreEqual(fpBeforeDismiss, bridgeHost.Bridge.Orchestrator.DecisionLog.ComputeFingerprint());

            // Re-select contact to re-open card, then test FULL_AUTONOMOUS opt-in guard on a full-auto unit
            var autoBinding = bridgeHost.Bridge.Registry.RegisterUnit(new EntityKey(12), "u-full-auto");
            bridgeHost.Bridge.Registry.MarkFriendlyMeshMember(autoBinding.TargetId);
            var fullAgent = bridgeHost.Bridge.Orchestrator.CreateAgentFromPreset(
                new AgentId("a-full"), PersonalityCatalog.All[0], AutonomyLevel.FullAutonomous);
            Assert.IsTrue(bridgeHost.Bridge.Orchestrator.TryAssignAgentController(autoBinding.Target, fullAgent, isFriendly: true).Allowed);
            DoctrineOverrideCommand.TryApply(bridgeHost.Bridge.Orchestrator, autoBinding.TargetId, "WeaponsFree", 5.0);

            bridgeHost.SelectUnit("u-full-auto");
            bridgeHost.SelectContact("c1");
            yield return null;

            Assert.IsFalse(acceptButton.enabledSelf);
            optInToggle.value = true;
            yield return null;
            Assert.IsTrue(acceptButton.enabledSelf);

            SendSubmit(acceptButton);
            yield return null;
            Assert.AreEqual(ordersBeforeDismiss + 1, bridgeHost.Bridge.Orchestrator.DecisionLog.PlayerOrders.Count);
            Assert.IsNotNull(statusLabel);
        }

        private static void BuildSupervisedAdviceTree(
            VisualElement root,
            out Label headerLabel,
            out ListView evidenceList,
            out Toggle optInToggle,
            out Button acceptButton,
            out Button dismissButton,
            out Label statusLabel)
        {
            var container = new VisualElement { name = "supervised-advice-root" };
            headerLabel = new Label { name = "supervised-advice-header" };
            var rationaleLabel = new Label { name = "supervised-advice-rationale" };
            var authorityLabel = new Label { name = "supervised-advice-authority" };
            evidenceList = new ListView { name = "supervised-advice-evidence-list" };
            optInToggle = new Toggle("Explicit FULL_AUTONOMOUS lethal engage opt-in") { name = "supervised-advice-optin-toggle" };
            acceptButton = new Button { name = "supervised-advice-accept", text = "ACCEPT" };
            dismissButton = new Button { name = "supervised-advice-dismiss", text = "DISMISS" };
            statusLabel = new Label { name = "supervised-advice-status" };

            container.Add(headerLabel);
            container.Add(rationaleLabel);
            container.Add(authorityLabel);
            container.Add(evidenceList);
            container.Add(optInToggle);
            container.Add(acceptButton);
            container.Add(dismissButton);
            container.Add(statusLabel);
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
