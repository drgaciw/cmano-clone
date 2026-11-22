// Sprint 2 DRG-408 PlayMode verification: Shared MessageCategoryPolicy, NotificationCoordinator,
// companion MapBalloonOverlayPanelHost (PickingMode.Ignore), and MessageLogPanelHost policy UI.
#if UNITY_5_3_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
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
    public sealed class Sprint2NotificationPolicyPlayModeTests
    {
        private sealed class StubWorld : ISimWorldSnapshot, IOrderSink
        {
            public double SimTime { get; set; } = 1.0;
            public int ContactCount => 1;
            public int ActiveEngagementCount => 0;
            public TargetId? PrimaryHostileContactId => new("hostile-1");
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
        public IEnumerator MapBalloonOverlay_renders_balloons_with_ignore_picking_and_single_dismiss_keeps_second_pause()
        {
            var go = new GameObject("sprint2-balloon-overlay-test");
            _cleanup.Add(go);

            var bridgeHost = go.AddComponent<DelegationBridgeHost>();
            var balloonDoc = CreateDocument(go);
            yield return null;

            bridgeHost.Bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
            bridgeHost.Bridge.Registry.RegisterUnit(new EntityKey(2), "hostile-1");
            bridgeHost.BeginExecution();

            var log = bridgeHost.Bridge.Orchestrator.DecisionLog;
            log.AppendEngagement(new EngagementRecord(
                0, 4.0, 4, new TargetId("u1"), 1, true, null, new TargetId("hostile-1"), "NSM"));
            log.AppendPlatformDamageChange(new PlatformDamageChangeRecord(
                0, 5.0, 5, new TargetId("u1"), 100.0, 80.0, "Hit", 1));
            log.AppendContactChange(new ContactChangeRecord(
                0, 6.0, 6, "u1", "c1", "hostile-1", "Detected", "Classified"));

            var world = new StubWorld { SimTime = 6.0 };
            bridgeHost.RunTick(world, world);

            var overlay = go.AddComponent<MapBalloonOverlayPanelHost>();
            overlay.enabled = false;
            BuildBalloonOverlayTree(balloonDoc.rootVisualElement, out var root, out var canvas, out _);
            SetPrivateField(overlay, "bridgeHost", bridgeHost);
            overlay.enabled = true;
            yield return null;

            Assert.GreaterOrEqual(balloonDoc.sortingOrder, 1);
            Assert.Less(balloonDoc.sortingOrder, 10);
            AssertAllIgnorePicking(root);
            Assert.IsTrue(bridgeHost.HasUnresolvedNotificationPause);
            Assert.IsTrue(bridgeHost.Session!.IsSimPaused);
            Assert.GreaterOrEqual(bridgeHost.ActiveMapBalloons.Count, 2);
            Assert.GreaterOrEqual(canvas.childCount, 1);

            // Dismiss one alert: unresolved pause from remaining pause alert MUST persist.
            var firstBalloonSeq = bridgeHost.ActiveMapBalloons[0].SequenceId;
            Assert.IsTrue(bridgeHost.TryDismissMessageAlert(firstBalloonSeq));
            yield return null;
            AssertAllIgnorePicking(root);
            Assert.IsTrue(bridgeHost.HasUnresolvedNotificationPause);
            Assert.IsFalse(bridgeHost.TryResumeSim(explicitOverride: false, out _));
        }

        [UnityTest]
        public IEnumerator ReplayViewer_attached_blocks_policy_edits_and_suppresses_fogged_alerts()
        {
            var go = new GameObject("sprint2-policy-replay-fog-test");
            _cleanup.Add(go);

            var bridgeHost = go.AddComponent<DelegationBridgeHost>();
            var doc = CreateDocument(go);
            yield return null;

            SetDelegationFogScenarioPolicy(bridgeHost.Bridge.Orchestrator);
            bridgeHost.SetMessageCategoryRule("AGENT_DECISION", new MessageCategoryRule(true, true, true, true));

            var logPanel = go.AddComponent<MessageLogPanelHost>();
            logPanel.enabled = false;
            BuildMessageLogTree(doc.rootVisualElement);
            SetPrivateField(logPanel, "bridgeHost", bridgeHost);
            logPanel.enabled = true;

            bridgeHost.BeginExecution();
            bridgeHost.Bridge.Orchestrator.DecisionLog.Append(new DecisionRecord(
                1.0, new AgentId("full-agent"), new TargetId("u1"), AutonomyLevel.FullAutonomous,
                OrderKind.Engage, System.Array.Empty<ScoredIntent>(), "hidden", 1, 10, 0.1, SimTick: 1));
            var world = new StubWorld { SimTime = 2.0 };
            bridgeHost.RunTick(world, world);
            yield return null;

            Assert.IsFalse(bridgeHost.HasUnresolvedNotificationPause);
            Assert.IsEmpty(bridgeHost.ActiveMapBalloons);

            bridgeHost.Bridge.AttachReplayViewer = true;
            bridgeHost.Bridge.Orchestrator.DecisionLog.AppendPlatformDamageChange(new PlatformDamageChangeRecord(
                0, 3.0, 3, new TargetId("u1"), 100.0, 50.0, "Hit", 2));
            bridgeHost.RunTick(new StubWorld { SimTime = 3.0 }, world);
            yield return null;

            Assert.IsFalse(bridgeHost.HasUnresolvedNotificationPause);
            Assert.IsFalse(bridgeHost.SetMessageCategoryRule("MISSION", new MessageCategoryRule(false, false, false, false)));
        }

        [UnityTest]
        public IEnumerator NonPause_MapBalloons_surface_in_alert_banner_for_dismiss_and_muted_FUEL_filters_before_windowing()
        {
            var go = new GameObject("sprint2-nonpause-dismiss-and-filter-test");
            _cleanup.Add(go);

            var bridgeHost = go.AddComponent<DelegationBridgeHost>();
            var doc = CreateDocument(go);
            yield return null;

            bridgeHost.Bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
            bridgeHost.Bridge.Registry.RegisterUnit(new EntityKey(2), "hostile-1");
            bridgeHost.SetMessageCategoryRule("FUEL", new MessageCategoryRule(false, false, false, false));

            var logPanel = go.AddComponent<MessageLogPanelHost>();
            logPanel.enabled = false;
            BuildMessageLogTree(doc.rootVisualElement);
            SetPrivateField(logPanel, "bridgeHost", bridgeHost);
            logPanel.enabled = true;

            bridgeHost.BeginExecution();
            var log = bridgeHost.Bridge.Orchestrator.DecisionLog;
            log.AppendEngagement(new EngagementRecord(
                0, 4.0, 4, new TargetId("u1"), 1, true, null, new TargetId("hostile-1"), "NSM"));
            log.AppendEngagementOutcome(new EngagementOutcomeRecord(
                0, 4.5, 5, new TargetId("u1"), new TargetId("hostile-1"), 1, "Hit", 0.25));
            for (var i = 0; i < 14; i++)
            {
                log.AppendFuelStateChange(new FuelStateChangeRecord(
                    0, 5.0 + i, (ulong)(6 + i), new TargetId("u1"), "Nominal", "Joker", 500.0 - i));
            }

            bridgeHost.RunTick(new StubWorld { SimTime = 20.0 }, new StubWorld());
            yield return null;

            Assert.IsFalse(bridgeHost.HasUnresolvedNotificationPause);
            Assert.AreEqual(2, bridgeHost.ActiveMapBalloons.Count);
            Assert.GreaterOrEqual(logPanel.PanelState.Rows.Count, 2);

            var banner = doc.rootVisualElement.Q<VisualElement>("message-log-alert-banner");
            Assert.IsNotNull(banner);
            Assert.AreEqual(DisplayStyle.Flex, banner!.style.display.value);

            Button? dismissBtn1 = null;
            foreach (var btn in banner.Query<Button>().ToList())
            {
                if (btn.text == "DISMISS")
                {
                    dismissBtn1 = btn;
                    break;
                }
            }

            Assert.IsNotNull(dismissBtn1);
            SendSubmit(dismissBtn1!);
            yield return null;
            Assert.AreEqual(1, bridgeHost.ActiveMapBalloons.Count);

            Button? dismissBtn2 = null;
            foreach (var btn in banner.Query<Button>().ToList())
            {
                if (btn.text == "DISMISS")
                {
                    dismissBtn2 = btn;
                    break;
                }
            }

            Assert.IsNotNull(dismissBtn2);
            SendSubmit(dismissBtn2!);
            yield return null;
            Assert.AreEqual(0, bridgeHost.ActiveMapBalloons.Count);
            Assert.AreEqual(DisplayStyle.None, banner.style.display.value);
        }

        private static void SendSubmit(VisualElement target)
        {
            using var submit = NavigationSubmitEvent.GetPooled();
            submit.target = target;
            target.SendEvent(submit);
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

        private static void BuildBalloonOverlayTree(
            VisualElement documentRoot,
            out VisualElement root,
            out VisualElement canvas,
            out VisualElement strip)
        {
            root = new VisualElement { name = MapBalloonOverlayProjection.RootElementName, pickingMode = PickingMode.Ignore };
            strip = new VisualElement { name = MapBalloonOverlayProjection.UnanchoredStripElementName, pickingMode = PickingMode.Ignore };
            canvas = new VisualElement { name = MapBalloonOverlayProjection.CanvasElementName, pickingMode = PickingMode.Ignore };
            root.Add(strip);
            root.Add(canvas);
            documentRoot.Add(root);
        }

        private static void BuildMessageLogTree(VisualElement documentRoot)
        {
            var root = new VisualElement { name = "message-log-root" };
            root.Add(new Label("MESSAGE LOG") { name = "message-log-title" });
            root.Add(new Button { name = "message-log-policy-toggle", text = "POLICY" });
            root.Add(new VisualElement { name = "message-log-alert-banner" });
            root.Add(new VisualElement { name = "message-log-policy-drawer" });
            root.Add(new Label { name = "message-log-onboarding-hint" });
            root.Add(new ListView { name = "message-list" });
            root.Add(new VisualElement { name = "message-log-deeplink-drawer" });
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

        private static void SetDelegationFogScenarioPolicy(DelegationOrchestrator orchestrator)
        {
            var policyProp = orchestrator.GetType().GetProperty("ScenarioPolicy", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(policyProp, "Missing DelegationOrchestrator.ScenarioPolicy property.");
            var policyType = policyProp!.PropertyType;
            var simAsm = policyType.Assembly;

            var effectivePolicyType = simAsm.GetType("ProjectAegis.Sim.Policy.EffectivePolicy");
            Assert.IsNotNull(effectivePolicyType, "Missing EffectivePolicy type.");
            var defaultFreeProp = effectivePolicyType!.GetProperty("DefaultFree", BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(defaultFreeProp, "Missing EffectivePolicy.DefaultFree property.");
            var defaultFree = defaultFreeProp!.GetValue(null);

            var playerInfoModelType = simAsm.GetType("ProjectAegis.Sim.Scenario.PlayerInfoModel");
            Assert.IsNotNull(playerInfoModelType, "Missing PlayerInfoModel enum.");
            var delegationFog = System.Enum.Parse(playerInfoModelType!, "DelegationFog");

            var ctor = policyType.GetConstructors(BindingFlags.Instance | BindingFlags.Public)[0];
            var parms = ctor.GetParameters();
            var args = new object?[parms.Length];
            args[0] = defaultFree;
            for (var i = 1; i < parms.Length; i++)
            {
                args[i] = parms[i].Name == "playerInfoModel"
                    ? delegationFog
                    : (parms[i].HasDefaultValue ? parms[i].DefaultValue : null);
            }

            policyProp.SetValue(orchestrator, ctor.Invoke(args));
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
