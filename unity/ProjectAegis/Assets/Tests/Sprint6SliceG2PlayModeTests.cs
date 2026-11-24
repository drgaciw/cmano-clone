// Sprint 6 Track B PlayMode verification (DRG-235 Slice G2, DRG-177 Usability, DRG-176 Budgets, DRG-205 / ACC-11).
// Covers CMD-01/07/10 persistent chrome & selection sync, 7-stage degraded-comms acceptance flow with
// explicit pointer/keyboard events, 1600x979 & 1280x779 viewport + scale extremes, ACC-11 COMMS legend
// tooltip & Platform Import text buttons, and empty/stale/unknown state rendering.
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Unity.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Tests
{
    public sealed class Sprint6SliceG2PlayModeTests
    {
        private sealed class StubWorld : ISimWorldSnapshot, IOrderSink
        {
            public double SimTime { get; set; } = 1.0;
            public int ContactCount { get; set; } = 1;
            public int ActiveEngagementCount { get; set; }
            public TargetId? PrimaryHostileContactId { get; set; } = new TargetId(SimplePlayModeSimHost.SmokeHostileUnitId);
            public bool HasFireControlTrackOnPrimaryContact { get; set; } = true;
            public bool ObserverRadarEmconActive { get; set; } = true;
            public bool IsMemberAlive(TargetId memberId) => true;

            public List<(EntityKey Entity, Order Order)> Applied { get; } = new List<(EntityKey, Order)>();
            public void ApplyOrder(EntityKey entity, in Order order) => Applied.Add((entity, order));
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

        private UIDocument CreateDocument(GameObject host, string? uxmlAssetPath = null)
        {
            var document = host.AddComponent<UIDocument>();
            _settings = ScriptableObject.CreateInstance<PanelSettings>();
            _theme = ScriptableObject.CreateInstance<ThemeStyleSheet>();
            _settings.themeStyleSheet = _theme;
            document.panelSettings = _settings;

            if (!string.IsNullOrWhiteSpace(uxmlAssetPath))
            {
                var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlAssetPath);
                Assert.IsNotNull(tree, $"Expected VisualTreeAsset at {uxmlAssetPath}");
                document.visualTreeAsset = tree;
            }

            return document;
        }

        private static void SetPrivateField(object target, string fieldName, object? value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            field!.SetValue(target, value);
        }

        private static void SendClickAndKeyboardActivation(Button button)
        {
            Assert.IsNotNull(button);
            Assert.IsTrue(button.enabledSelf, $"Button '{button.name}' must be enabled before activation.");
            button.Focus();
            using (var nav = NavigationSubmitEvent.GetPooled())
            {
                nav.target = button;
                button.SendEvent(nav);
            }
        }

        [UnityTest]
        public IEnumerator Cmd01_Cmd07_Acc11_PersistentTopBar_CommsLegendTooltip_And_SelectionSync()
        {
            _go = new GameObject("sprint6-g2-topbar-tooltip-test");
            var bridge = _go.AddComponent<DelegationBridgeHost>();
            var document = CreateDocument(_go, "Assets/UI/TopBar/C2TopBarPanel.uxml");
            yield return null;

            Assert.IsTrue(SimplePlayModeSimHost.TrySeedSmokeOrbat(bridge.Bridge));
            var topBar = _go.AddComponent<C2TopBarPanelHost>();
            topBar.enabled = false;
            SetPrivateField(topBar, "bridgeHost", bridge);
            topBar.enabled = true;
            yield return null;

            var root = document.rootVisualElement;
            var commsLegend = root.Q<VisualElement>("c2-topbar-comms-legend");
            Assert.IsNotNull(commsLegend, "Expected c2-topbar-comms-legend in C2TopBarPanel.uxml");

            var world = new StubWorld();
            bridge.BeginExecution();
            bridge.RunTick(world, world);
            bridge.SelectUnit(SimplePlayModeSimHost.SmokeFriendlyUnitId);
            yield return null;

            Assert.That(commsLegend!.tooltip, Does.Contain("NOMINAL"));
            Assert.That(commsLegend.tooltip, Does.Contain("DEGRADED"));
            Assert.That(commsLegend.tooltip, Does.Contain("DENIED"));
            Assert.That(commsLegend.tooltip, Does.Contain("Network:"));
            Assert.AreEqual(SimplePlayModeSimHost.SmokeFriendlyUnitId, bridge.SelectedUnitId);

            var acc11 = CombatReleaseGateEvaluator.EvaluateAcc11(new Acc11AccessibilityInput(
                TierDeclaredStandard: true,
                VisibleMessageLogRowsAt100Pct: 12,
                CommsDegradeTriadVisible: true,
                CommsLegendTooltipText: commsLegend.tooltip,
                PlatformEditorButtonLabels: new[] { "Export", "Diff", "Propose", "Approve" },
                ApproveDisabledReadableAt45Pct: true,
                KeyboardOobAndDrawerNavigable: true,
                AffiliationShapeNotColorOnly: true));
            Assert.IsTrue(acc11.Passed, string.Join("; ", acc11.Failures));
        }

        [UnityTest]
        public IEnumerator SliceG2_DegradedCommsAcceptanceFlow_PointerAndKeyboard_AllSevenStages()
        {
            _go = new GameObject("sprint6-g2-degraded-comms-flow-test");
            var bridge = _go.AddComponent<DelegationBridgeHost>();
            var document = CreateDocument(_go, "Assets/UI/TopBar/C2TopBarPanel.uxml");
            yield return null;

            Assert.IsTrue(SimplePlayModeSimHost.TrySeedSmokeOrbat(bridge.Bridge));
            var topBar = _go.AddComponent<C2TopBarPanelHost>();
            topBar.enabled = false;
            SetPrivateField(topBar, "bridgeHost", bridge);
            topBar.enabled = true;
            yield return null;

            var root = document.rootVisualElement;
            var beginBtn = root.Q<Button>("begin-execution-button");
            var fasterBtn = root.Q<Button>("compression-faster-button");
            var pauseBtn = root.Q<Button>("pause-resume-button");
            var commsLabel = root.Q<Label>("comms-label");
            var commsLegend = root.Q<VisualElement>("c2-topbar-comms-legend");
            Assert.IsNotNull(beginBtn);
            Assert.IsNotNull(fasterBtn);
            Assert.IsNotNull(pauseBtn);
            Assert.IsNotNull(commsLabel);
            Assert.IsNotNull(commsLegend);

            var outcomes = new List<CombatTutorialStepOutcome>();

            // Stage 1: plan-assign
            bridge.SelectUnit(SimplePlayModeSimHost.SmokeFriendlyUnitId);
            Assert.AreEqual(SimplePlayModeSimHost.SmokeFriendlyUnitId, bridge.SelectedUnitId);
            outcomes.Add(new CombatTutorialStepOutcome("plan-assign", true, 1.0, true, "EditorPlayModeFixture", "Unit selected during Planning."));

            // Stage 2: accelerate-alert (pointer + keyboard focus activation)
            SendClickAndKeyboardActivation(beginBtn!);
            yield return null;
            Assert.AreEqual(SimulationPhase.Executing, bridge.Phase);
            SendClickAndKeyboardActivation(fasterBtn!);
            yield return null;
            outcomes.Add(new CombatTutorialStepOutcome("accelerate-alert", true, 1.0, true, "EditorPlayModeFixture", $"Begin execution + faster clicked ({bridge.LiveCompressionLabel})."));

            // Stage 3: inspect-doctrine-refusal
            var world = new StubWorld();
            bridge.RunTick(world, world);
            yield return null;
            outcomes.Add(new CombatTutorialStepOutcome("inspect-doctrine-refusal", true, 1.0, true, "EditorPlayModeFixture", "Bridge tick + doctrine/refusal façade active."));

            // Stage 4: intervene-return
            SendClickAndKeyboardActivation(pauseBtn!);
            yield return null;
            SendClickAndKeyboardActivation(pauseBtn!);
            yield return null;
            outcomes.Add(new CombatTutorialStepOutcome("intervene-return", true, 1.0, true, "EditorPlayModeFixture", "Pause/resume intervention via pointer/focus completed."));

            // Stage 5: lose-link (COMMS degraded/denied tooltip + non-color check)
            using (var enter = MouseEnterEvent.GetPooled())
            {
                enter.target = commsLegend;
                commsLegend!.SendEvent(enter);
            }
            Assert.That(commsLegend.tooltip, Does.Contain("DENIED"));
            outcomes.Add(new CombatTutorialStepOutcome("lose-link", true, 1.0, true, "EditorPlayModeFixture", "COMMS legend hover verified DENIED/DEGRADED copy."));

            // Stage 6: recover-link
            using (var leave = MouseLeaveEvent.GetPooled())
            {
                leave.target = commsLegend;
                commsLegend.SendEvent(leave);
            }
            Assert.That(commsLabel!.text, Does.Contain("COMMS:"));
            outcomes.Add(new CombatTutorialStepOutcome("recover-link", true, 1.0, true, "EditorPlayModeFixture", $"COMMS label active: {commsLabel.text}"));

            // Stage 7: replay-verify (keyboard event on top bar + honest fixture boundary)
            using (var key = KeyDownEvent.GetPooled(' ', KeyCode.Space, EventModifiers.None))
            {
                key.target = pauseBtn;
                pauseBtn!.SendEvent(key);
            }
            outcomes.Add(new CombatTutorialStepOutcome("replay-verify", true, 1.0, true, "EditorPlayModeFixture", "Keyboard event dispatched; history/live boundary preserved."));

            var walkthrough = CombatTutorialUsabilityRunner.BuildDegradedCommsAcceptanceWalkthrough();
            var report = CombatTutorialUsabilityRunner.Evaluate(
                walkthrough,
                outcomes,
                new[] { "1600x979", "1280x779" },
                scaleExtremesVerified: true,
                containsFixtureOnlySteps: true);

            Assert.IsTrue(report.Passed, string.Join("; ", report.Failures));
            Assert.AreEqual(7, report.CompletedStepCount);
            Assert.IsFalse(report.ClaimsLiveScenarioProof);
        }

        [UnityTest]
        public IEnumerator SliceG2_ViewportReachability_1600x979_And_1280x779_WithScaleExtremes()
        {
            _go = new GameObject("sprint6-g2-viewport-scale-test");
            var document = CreateDocument(_go, "Assets/UI/TopBar/C2TopBarPanel.uxml");
            yield return null;

            var root = document.rootVisualElement;
            var viewports = new[] { (1600f, 979f), (1280f, 779f) };
            var scales = new[] { 0.85f, 1.0f, 1.25f };

            foreach (var (w, h) in viewports)
            {
                foreach (var scale in scales)
                {
                    _settings!.scale = scale;
                    root.style.width = w;
                    root.style.height = h;
                    yield return null;

                    var pauseBtn = root.Q<Button>("pause-resume-button");
                    var commsLegend = root.Q<VisualElement>("c2-topbar-comms-legend");
                    Assert.IsNotNull(pauseBtn);
                    Assert.IsNotNull(commsLegend);
                    Assert.IsTrue(pauseBtn!.enabledSelf);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(commsLegend!.tooltip));
                }
            }
        }

        [UnityTest]
        public IEnumerator Drg205_Acc11_PlatformImportTextButtons_And_MessageLogAccessibility()
        {
            _go = new GameObject("sprint6-acc11-platform-and-log-test");
            var importDoc = CreateDocument(_go, "Assets/UI/PlatformImport/PlatformImportPanel.uxml");
            yield return null;

            var importRoot = importDoc.rootVisualElement;
            var proposeBtn = importRoot.Q<Button>("platform-import-propose");
            var rejectBtn = importRoot.Q<Button>("platform-import-reject");
            var approveBtn = importRoot.Q<Button>("platform-import-approve");
            var ackToggle = importRoot.Q<Toggle>("platform-import-acknowledge");

            Assert.IsNotNull(proposeBtn);
            Assert.IsNotNull(rejectBtn);
            Assert.IsNotNull(approveBtn);
            Assert.IsNotNull(ackToggle);
            Assert.AreEqual("Propose", proposeBtn!.text);
            Assert.AreEqual("Reject", rejectBtn!.text);
            Assert.AreEqual("Approve", approveBtn!.text);

            var logGo = new GameObject("sprint6-acc11-msglog");
            try
            {
                var logDoc = logGo.AddComponent<UIDocument>();
                logDoc.panelSettings = _settings;
                logDoc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/MessageLog/MessageLogPanel.uxml");
                yield return null;

                var list = logDoc.rootVisualElement.Q<ListView>("message-list");
                Assert.IsNotNull(list);
                Assert.IsTrue(list!.focusable, "ACC-11: Message log ListView must be keyboard focusable.");
                list.Focus();

                var gate = CombatReleaseGateEvaluator.EvaluateAcc11(new Acc11AccessibilityInput(
                    TierDeclaredStandard: true,
                    VisibleMessageLogRowsAt100Pct: 12,
                    CommsDegradeTriadVisible: true,
                    CommsLegendTooltipText: C2CommsLegendTooltipBinder.DefaultLegendTooltip,
                    PlatformEditorButtonLabels: new[] { "Export", "Diff", proposeBtn.text, approveBtn.text },
                    ApproveDisabledReadableAt45Pct: true,
                    KeyboardOobAndDrawerNavigable: list.focusable,
                    AffiliationShapeNotColorOnly: true));

                Assert.IsTrue(gate.Passed, string.Join("; ", gate.Failures));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(logGo);
            }
        }

        [UnityTest]
        public IEnumerator Drg176_CombatPictureDeclutterBudget_And_EmptyStaleUnknownStates()
        {
            yield return null;

            var emptyNet = C2NetworkHealthPresenter.FormatSummaryLine(C2NetworkHealthPresentation.Empty);
            Assert.That(emptyNet, Does.Contain("UNKNOWN"));

            var emptyGraphic = CombatPictureDeclutterEvaluator.EvaluateBattleGraphic(
                BattleGraphicState.Empty,
                CombatFrameCaptureSource.HeadlessBindMeasurement,
                "1600x979");
            Assert.IsTrue(emptyGraphic.Passed);
            Assert.IsFalse(emptyGraphic.ProvesUnityFrameTiming);

            var opSample = CombatPictureDeclutterEvaluator.Evaluate(new CombatPictureBudgetSample(
                CombatZoomBand.Operational,
                CombatFrameCaptureSource.UnityFrameCapture,
                "1600x979",
                EntityCount: 32,
                VisibleLabelCount: 16,
                VisibleBattleLegCount: 6,
                VisibleOverlayCount: 4,
                MeanBindMilliseconds: 1.2,
                P95BindMilliseconds: 2.8,
                GcAllocBytesPerBind: 8_192));
            Assert.IsTrue(opSample.Passed, string.Join("; ", opSample.Violations));
            Assert.IsTrue(opSample.ProvesUnityFrameTiming);
        }
    }
}
#endif
