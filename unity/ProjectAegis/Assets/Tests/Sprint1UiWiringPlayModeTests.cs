// Sprint 1 Track B PlayMode verification (DRG-773, DRG-717, DRG-370, DRG-351).
// Covers C2TopBarPanelHost loop-policy/fog projection bind, RightUnitPanelHost live commit
// strip/bark wiring, and RuntimeTooltipPresenter Play Mode hover/focus visibility.
#if UNITY_5_3_OR_NEWER
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Unity.Runtime;

namespace ProjectAegis.Unity.Tests
{
    public sealed class Sprint1UiWiringPlayModeTests
    {
        private sealed class StubWorld : ISimWorldSnapshot, IOrderSink
        {
            public double SimTime { get; set; } = 1;
            public int ContactCount => 1;
            public int ActiveEngagementCount => 0;
            public TargetId? PrimaryHostileContactId => new(SimplePlayModeSimHost.SmokeHostileUnitId);
            public bool HasFireControlTrackOnPrimaryContact { get; set; } = true;
            public bool ObserverRadarEmconActive { get; set; } = true;
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
        public IEnumerator Top_bar_binds_fog_mode_and_loop_policy_chrome_via_projection()
        {
            _go = new GameObject("sprint1-topbar-loop-policy-test");
            var bridge = _go.AddComponent<DelegationBridgeHost>();
            var document = CreateDocument(_go);
            yield return null;

            var topBar = _go.AddComponent<C2TopBarPanelHost>();
            topBar.enabled = false;
            var root = BuildTopBarTree(document.rootVisualElement, out var fogLabel, out var editPolicyLabel);
            SetPrivateField(topBar, "bridgeHost", bridge);
            topBar.enabled = true;
            yield return null;

            var expected = LoopPolicyChromeBinder.Project(bridge.Bridge);
            Assert.AreEqual(expected.Fog.Label, fogLabel.text);
            Assert.AreEqual(expected.EditPolicy.Label, editPolicyLabel.text);
            Assert.AreEqual(expected, topBar.LastLoopPolicyChrome);
            Assert.IsTrue(fogLabel.ClassListContains(expected.Fog.CssClass));
            Assert.IsTrue(editPolicyLabel.ClassListContains(expected.EditPolicy.CssClass));
            Assert.IsNotNull(root);
        }

        [UnityTest]
        public IEnumerator Right_unit_panel_renders_commit_strip_and_dropped_bark_live()
        {
            _go = new GameObject("sprint1-right-unit-commit-strip-test");
            var bridge = _go.AddComponent<DelegationBridgeHost>();
            var document = CreateDocument(_go);
            Assert.IsTrue(SimplePlayModeSimHost.TrySeedSmokeOrbat(bridge.Bridge));
            bridge.BeginExecution();

            var world = new StubWorld { HasFireControlTrackOnPrimaryContact = true, ObserverRadarEmconActive = true };
            bridge.RunTick(world, world);
            bridge.SelectUnit(SimplePlayModeSimHost.SmokeFriendlyUnitId);
            yield return null;

            var panelHost = _go.AddComponent<RightUnitPanelHost>();
            panelHost.enabled = false;
            BuildRightUnitTree(
                document.rootVisualElement,
                out var constraintsLabel,
                out var costLabel,
                out var barkLabel,
                out _);
            SetPrivateField(panelHost, "bridgeHost", bridge);
            panelHost.enabled = true;
            yield return null;

            StringAssert.StartsWith("COMMIT: READY", constraintsLabel.text);
            StringAssert.StartsWith("COST: 1 rd | MAG ", costLabel.text);
            Assert.IsTrue(constraintsLabel.ClassListContains(CommitConstraintStripCueClasses.Ready));
            Assert.AreEqual(DisplayStyle.None, barkLabel.style.display.value);

            bridge.Bridge.Orchestrator.DecisionLog.Append(OrderLogEntryFactories.FromEngagement(
                new EngagementRecord(
                    SequenceId: 0,
                    SimTime: 2.0,
                    SimTick: 2,
                    ShooterTargetId: new TargetId(SimplePlayModeSimHost.SmokeFriendlyUnitId),
                    EngagementId: 99,
                    Launched: false,
                    AbortReasonCode: "DLZ_OUT")));
            yield return null;

            Assert.AreEqual(DisplayStyle.Flex, barkLabel.style.display.value);
            Assert.AreEqual("u1 DROPPED: DLZ_OUT", barkLabel.text);
            Assert.IsTrue(barkLabel.ClassListContains(CommitRefusalBarkCueClasses.Dropped));
            Assert.IsNotNull(panelHost.LastCommitBark);
        }

        [UnityTest]
        public IEnumerator Runtime_tooltip_presenter_shows_and_hides_on_focus_and_hover_in_play_mode()
        {
            _go = new GameObject("sprint1-runtime-tooltip-test");
            var bridge = _go.AddComponent<DelegationBridgeHost>();
            var document = CreateDocument(_go);
            Assert.IsTrue(SimplePlayModeSimHost.TrySeedSmokeOrbat(bridge.Bridge));
            bridge.BeginExecution();

            var world = new StubWorld { HasFireControlTrackOnPrimaryContact = false, ObserverRadarEmconActive = true };
            bridge.RunTick(world, world);
            bridge.SelectUnit(SimplePlayModeSimHost.SmokeFriendlyUnitId);
            yield return null;

            var panelHost = _go.AddComponent<RightUnitPanelHost>();
            panelHost.enabled = false;
            BuildRightUnitTree(
                document.rootVisualElement,
                out _,
                out _,
                out _,
                out var fireSingleRow);
            SetPrivateField(panelHost, "bridgeHost", bridge);
            panelHost.enabled = true;
            yield return null;

            var presenter = panelHost.TooltipPresenter;
            Assert.IsNotNull(presenter.TooltipElement);
            Assert.IsFalse(string.IsNullOrWhiteSpace(fireSingleRow.tooltip),
                "Denied weapon row must carry a non-empty abort tooltip.");
            Assert.IsFalse(presenter.IsVisible);

            fireSingleRow.Focus();
            yield return null;

            Assert.IsTrue(presenter.IsVisible, "Tooltip popup must become visible on focus.");
            Assert.AreEqual(fireSingleRow.tooltip, presenter.CurrentText);
            Assert.AreEqual(DisplayStyle.Flex, presenter.TooltipElement!.style.display.value);

            fireSingleRow.Blur();
            yield return null;

            Assert.IsFalse(presenter.IsVisible, "Tooltip popup must hide on blur/focus-out.");
            Assert.AreEqual(DisplayStyle.None, presenter.TooltipElement.style.display.value);

            using (var mouseEnter = MouseEnterEvent.GetPooled())
            {
                mouseEnter.target = fireSingleRow;
                fireSingleRow.SendEvent(mouseEnter);
            }

            Assert.IsTrue(presenter.IsVisible, "Tooltip popup must become visible on pointer hover.");
            Assert.AreEqual(fireSingleRow.tooltip, presenter.CurrentText);

            using (var mouseLeave = MouseLeaveEvent.GetPooled())
            {
                mouseLeave.target = fireSingleRow;
                fireSingleRow.SendEvent(mouseLeave);
            }

            Assert.IsFalse(presenter.IsVisible, "Tooltip popup must hide on pointer leave.");
        }

        private static VisualElement BuildTopBarTree(
            VisualElement documentRoot,
            out Label fogLabel,
            out Label editPolicyLabel)
        {
            var root = new VisualElement { name = "c2-topbar-root" };
            root.Add(new Label { name = "sim-time-label" });
            root.Add(new Label { name = "phase-label" });
            root.Add(new Label { name = "compression-label" });
            root.Add(new Label { name = "mode-label" });
            fogLabel = new Label { name = "fog-mode-label" };
            root.Add(fogLabel);
            editPolicyLabel = new Label { name = "edit-policy-label" };
            root.Add(editPolicyLabel);
            root.Add(new Label { name = "score-label" });
            documentRoot.Add(root);
            return root;
        }

        private static void BuildRightUnitTree(
            VisualElement documentRoot,
            out Label constraintsLabel,
            out Label costLabel,
            out Label barkLabel,
            out VisualElement fireSingleRow)
        {
            var root = new VisualElement { name = "unit-detail-root" };
            root.Add(new Label { name = "unit-id-line" });
            root.Add(new Label { name = "status-line" });
            root.Add(new Label { name = "comms-line" });
            root.Add(new Label { name = "magazine-line" });
            root.Add(new Label { name = "emcon-line" });
            root.Add(new Label { name = "doctrine-line" });
            root.Add(new Label { name = "fuel-line" });
            root.Add(new Label { name = "dlz-line" });
            root.Add(new Label { name = "wra-salvo-line" });
            root.Add(new Label { name = "engage-line" });
            root.Add(new Label { name = "attack-options-line" });

            var menu = new VisualElement { name = "attack-menu" };
            fireSingleRow = new VisualElement { name = WeaponAbortTooltipBinder.FireSingleRowName, focusable = true };
            fireSingleRow.Add(new Button { name = "attack-fire-single", text = "Fire 1 round" });
            menu.Add(fireSingleRow);

            var fireSalvoRow = new VisualElement { name = WeaponAbortTooltipBinder.FireSalvoRowName, focusable = true };
            fireSalvoRow.Add(new Button { name = "attack-fire-salvo", text = "Fire salvo" });
            menu.Add(fireSalvoRow);

            menu.Add(new Button { name = "attack-hold-fire", text = "Hold fire" });
            root.Add(menu);

            constraintsLabel = new Label { name = CommitConstraintStripBinder.ConstraintElementName };
            costLabel = new Label { name = CommitConstraintStripBinder.CostElementName };
            barkLabel = new Label { name = CommitRefusalBarkBinder.BarkElementName };
            root.Add(constraintsLabel);
            root.Add(costLabel);
            root.Add(barkLabel);
            root.Add(new Label { name = "contact-line" });
            documentRoot.Add(root);
        }

        private static void SetPrivateField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
    }
}
#endif
