#if UNITY_5_3_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Unity.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Tests
{
    public sealed class Sprint2UnitControlPlayModeTests
    {
        private sealed class StubWorld : ISimWorldSnapshot, IOrderSink
        {
            public double SimTime { get; set; } = 1.0;
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
        public IEnumerator Right_unit_panel_take_control_pause_resume_and_return_to_agent_in_play_mode()
        {
            _go = new GameObject("sprint2-unit-control-test");
            var bridgeHost = _go.AddComponent<DelegationBridgeHost>();
            var document = CreateDocument(_go);
            yield return null;

            Assert.IsTrue(SimplePlayModeSimHost.TrySeedSmokeOrbat(bridgeHost.Bridge));
            var u2Binding = bridgeHost.Bridge.Registry.RegisterUnit(new EntityKey(10), "u-agent");
            bridgeHost.Bridge.Registry.MarkFriendlyMeshMember(u2Binding.TargetId);
            var agent = bridgeHost.Bridge.Orchestrator.CreateAgentFromPreset(
                new AgentId("a-play"), PersonalityCatalog.All[0], AutonomyLevel.FullAutonomous);
            var assign = bridgeHost.Bridge.Orchestrator.TryAssignAgentController(u2Binding.Target, agent, isFriendly: true);
            Assert.IsTrue(assign.Allowed, assign.DenialReason);
            bridgeHost.BeginExecution();

            var world = new StubWorld { SimTime = 1.0 };
            bridgeHost.RunTick(world, world);
            bridgeHost.SelectUnit("u-agent");
            yield return null;

            var panelHost = _go.AddComponent<RightUnitPanelHost>();
            panelHost.enabled = false;
            BuildRightUnitControlTree(
                document.rootVisualElement,
                out var statusLabel,
                out var takeBtn,
                out var returnBtn,
                out var pauseBtn,
                out var resumeBtn,
                out var feedbackLabel);
            SetPrivateField(panelHost, "bridgeHost", bridgeHost);
            panelHost.enabled = true;
            yield return null;

            Assert.IsTrue(pauseBtn.enabledSelf);
            Assert.IsFalse(resumeBtn.enabledSelf);
            Assert.IsTrue(takeBtn.enabledSelf);
            Assert.IsFalse(returnBtn.enabledSelf);
            StringAssert.Contains("Agent", statusLabel.text);

            // Pause agent actions
            SendSubmit(pauseBtn);
            yield return null;
            Assert.IsTrue(agent.IsActionsPaused);
            Assert.IsFalse(pauseBtn.enabledSelf);
            Assert.IsTrue(resumeBtn.enabledSelf);

            var decisionsWhilePaused = bridgeHost.Bridge.Orchestrator.DecisionLog.Records.Count(r => r.TargetId == u2Binding.TargetId);
            world.SimTime = 5.0;
            bridgeHost.RunTick(world, world);
            yield return null;
            Assert.AreEqual(decisionsWhilePaused,
                bridgeHost.Bridge.Orchestrator.DecisionLog.Records.Count(r => r.TargetId == u2Binding.TargetId),
                "Paused agent must not emit new decisions on RunTick.");

            // Resume agent actions
            SendSubmit(resumeBtn);
            yield return null;
            Assert.IsFalse(agent.IsActionsPaused);
            Assert.IsTrue(pauseBtn.enabledSelf);
            Assert.IsFalse(resumeBtn.enabledSelf);

            // Take control & return to agent
            SendSubmit(takeBtn);
            yield return null;
            Assert.IsInstanceOf<HumanController>(u2Binding.Target.Slot.Active);
            Assert.IsFalse(takeBtn.enabledSelf);
            Assert.IsTrue(returnBtn.enabledSelf);

            SendSubmit(returnBtn);
            yield return null;
            Assert.AreSame(agent, u2Binding.Target.Slot.Active);
            Assert.IsTrue(takeBtn.enabledSelf);
            Assert.IsFalse(returnBtn.enabledSelf);
            Assert.IsNotNull(feedbackLabel);
        }

        [UnityTest]
        public IEnumerator Right_unit_panel_disables_control_buttons_with_reason_under_attach_replay_viewer()
        {
            _go = new GameObject("sprint2-unit-control-replay-test");
            var bridgeHost = _go.AddComponent<DelegationBridgeHost>();
            var document = CreateDocument(_go);
            yield return null;

            var u2Binding = bridgeHost.Bridge.Registry.RegisterUnit(new EntityKey(11), "u-agent-2");
            bridgeHost.Bridge.Registry.MarkFriendlyMeshMember(u2Binding.TargetId);
            var agent = bridgeHost.Bridge.Orchestrator.CreateAgentFromPreset(
                new AgentId("a-replay"), PersonalityCatalog.All[0], AutonomyLevel.FullAutonomous);
            Assert.IsTrue(bridgeHost.Bridge.Orchestrator.TryAssignAgentController(u2Binding.Target, agent, isFriendly: true).Allowed);
            bridgeHost.BeginExecution();
            var world = new StubWorld();
            bridgeHost.RunTick(world, world);
            bridgeHost.SelectUnit("u-agent-2");
            yield return null;

            var panelHost = _go.AddComponent<RightUnitPanelHost>();
            panelHost.enabled = false;
            BuildRightUnitControlTree(
                document.rootVisualElement,
                out var statusLabel,
                out var takeBtn,
                out var returnBtn,
                out var pauseBtn,
                out var resumeBtn,
                out _);
            SetPrivateField(panelHost, "bridgeHost", bridgeHost);
            panelHost.enabled = true;
            yield return null;

            bridgeHost.Bridge.AttachReplayViewer = true;
            var changesBefore = bridgeHost.Bridge.Orchestrator.DecisionLog.ControllerChanges.Count;
            yield return null;

            Assert.IsFalse(takeBtn.enabledSelf);
            Assert.IsFalse(returnBtn.enabledSelf);
            Assert.IsFalse(pauseBtn.enabledSelf);
            Assert.IsFalse(resumeBtn.enabledSelf);
            Assert.IsTrue(panelHost.LastControlState.IsDisabled);
            Assert.AreEqual(ObserverModeChromePresentation.ReplayAttachedReasonText, panelHost.LastControlState.DisabledReason);
            StringAssert.Contains("replay viewer attached", statusLabel.text.ToLowerInvariant());

            SendSubmit(pauseBtn);
            SendSubmit(takeBtn);
            yield return null;
            Assert.AreEqual(changesBefore, bridgeHost.Bridge.Orchestrator.DecisionLog.ControllerChanges.Count);
        }

        [UnityTest]
        public IEnumerator Right_unit_panel_clears_control_feedback_on_selection_change()
        {
            _go = new GameObject("sprint2-unit-control-feedback-clear-test");
            var bridgeHost = _go.AddComponent<DelegationBridgeHost>();
            var document = CreateDocument(_go);
            yield return null;

            Assert.IsTrue(SimplePlayModeSimHost.TrySeedSmokeOrbat(bridgeHost.Bridge));
            var u2Binding = bridgeHost.Bridge.Registry.RegisterUnit(new EntityKey(12), "u-agent-3");
            bridgeHost.Bridge.Registry.MarkFriendlyMeshMember(u2Binding.TargetId);
            var agent = bridgeHost.Bridge.Orchestrator.CreateAgentFromPreset(
                new AgentId("a-fb"), PersonalityCatalog.All[0], AutonomyLevel.FullAutonomous);
            Assert.IsTrue(bridgeHost.Bridge.Orchestrator.TryAssignAgentController(u2Binding.Target, agent, isFriendly: true).Allowed);
            bridgeHost.BeginExecution();
            var world = new StubWorld();
            bridgeHost.RunTick(world, world);
            bridgeHost.SelectUnit("u-agent-3");
            yield return null;

            var panelHost = _go.AddComponent<RightUnitPanelHost>();
            panelHost.enabled = false;
            BuildRightUnitControlTree(
                document.rootVisualElement,
                out _,
                out _,
                out _,
                out var pauseBtn,
                out _,
                out var feedbackLabel);
            SetPrivateField(panelHost, "bridgeHost", bridgeHost);
            panelHost.enabled = true;
            yield return null;

            SendSubmit(pauseBtn);
            yield return null;
            Assert.IsFalse(string.IsNullOrEmpty(feedbackLabel.text));

            bridgeHost.SelectUnit(SimplePlayModeSimHost.SmokeFriendlyUnitId);
            yield return null;
            Assert.IsTrue(string.IsNullOrEmpty(feedbackLabel.text),
                "Control feedback must clear when switching selected unit.");
        }

        private static void SendSubmit(VisualElement target)
        {
            using var submit = NavigationSubmitEvent.GetPooled();
            submit.target = target;
            target.SendEvent(submit);
        }

        private static void BuildRightUnitControlTree(
            VisualElement documentRoot,
            out Label statusLabel,
            out Button takeBtn,
            out Button returnBtn,
            out Button pauseBtn,
            out Button resumeBtn,
            out Label feedbackLabel)
        {
            var root = new VisualElement { name = "unit-detail-root" };
            root.Add(new Label { name = "unit-id-line" });
            root.Add(new Label { name = "status-line" });
            root.Add(new Label { name = "magazine-line" });
            root.Add(new Label { name = "emcon-line" });
            root.Add(new Label { name = "doctrine-line" });
            statusLabel = new Label { name = "unit-control-status" };
            takeBtn = new Button { name = "unit-take-control", text = "Take Control" };
            returnBtn = new Button { name = "unit-return-to-agent", text = "Return to Agent" };
            pauseBtn = new Button { name = "unit-pause-agent", text = "Pause Agent" };
            resumeBtn = new Button { name = "unit-resume-agent", text = "Resume Agent" };
            feedbackLabel = new Label { name = "unit-control-feedback" };
            root.Add(statusLabel);
            root.Add(takeBtn);
            root.Add(returnBtn);
            root.Add(pauseBtn);
            root.Add(resumeBtn);
            root.Add(feedbackLabel);
            documentRoot.Add(root);
        }

        private static void SetPrivateField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
    }
}
#endif
