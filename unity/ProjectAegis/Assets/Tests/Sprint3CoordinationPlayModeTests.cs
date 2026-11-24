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
    public sealed class Sprint3CoordinationPlayModeTests
    {
        private sealed class StubWorld : ISimWorldSnapshot, IOrderSink
        {
            private readonly Dictionary<string, bool> _alive;

            public StubWorld(Dictionary<string, bool> alive)
            {
                _alive = alive;
            }

            public double SimTime { get; set; } = 1.0;
            public int ContactCount => 1;
            public int ActiveEngagementCount => 0;
            public TargetId? PrimaryHostileContactId => new TargetId(PlayModeSmokeOrbatSeeder.HostileUnitId);
            public bool HasFireControlTrackOnPrimaryContact => true;
            public bool ObserverRadarEmconActive => true;
            public bool IsMemberAlive(TargetId memberId) => _alive.TryGetValue(memberId.Value, out var ok) && ok;

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
        public IEnumerator CoordinationPanelHost_displays_escort_sector_shared_targeting_and_submits_hold_withdraw_via_bridge_host()
        {
            _go = new GameObject("sprint3-coord-panel-test");
            var bridgeHost = _go.AddComponent<DelegationBridgeHost>();
            var document = CreateDocument(_go);
            yield return null;

            Assert.IsTrue(PlayModeSmokeOrbatSeeder.TrySeedWithEscortTaskGroup(bridgeHost.Bridge));
            bridgeHost.BeginExecution();

            var alive = new Dictionary<string, bool>
            {
                [PlayModeSmokeOrbatSeeder.FriendlyUnitId] = true,
                [PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId] = true,
                [PlayModeSmokeOrbatSeeder.HostileUnitId] = true,
            };
            var world = new StubWorld(alive) { SimTime = 1.0 };
            var wrapped = PlayModeSmokeCoordinationFactsProvider.WrapSnapshot(world);
            bridgeHost.RunTick(wrapped, world);
            yield return null;

            var panelHost = _go.AddComponent<CoordinationPanelHost>();
            panelHost.enabled = false;
            BuildCoordinationTree(
                document.rootVisualElement,
                out var boundariesList,
                out var blockReasonLine,
                out var holdBtn,
                out var withdrawBtn,
                out var reattackBtn,
                out var feedbackLine);
            SetPrivateField(panelHost, "bridgeHost", bridgeHost);
            panelHost.enabled = true;
            yield return null;

            Assert.AreEqual(CoordinationPanelHost.DefaultSortingOrder, document.sortingOrder);
            var groupView = panelHost.LastPresentation.FindGroup(PlayModeSmokeOrbatSeeder.TaskGroupId);
            Assert.IsNotNull(groupView);
            Assert.IsTrue(groupView!.Boundaries.Any(b => b.Kind == "Escort"));
            Assert.IsTrue(groupView.Boundaries.Any(b => b.Kind == "Sector"));
            Assert.IsTrue(groupView.Boundaries.Any(b => b.Kind == "SharedTarget"));
            Assert.GreaterOrEqual(boundariesList.childCount, 3);
            Assert.IsTrue(holdBtn.enabledSelf);
            Assert.IsTrue(withdrawBtn.enabledSelf);
            Assert.IsTrue(reattackBtn.enabledSelf);
            Assert.IsNotNull(blockReasonLine);

            // Submit Hold through CoordinationPanelHost -> DelegationBridgeHost.SubmitGroupDecision
            var appliedBeforeSubmit = world.Applied.Count;
            SendSubmit(holdBtn);
            yield return null;

            Assert.IsNotNull(panelHost.LastFeedback);
            StringAssert.StartsWith("Queued Hold:", panelHost.LastFeedback);
            StringAssert.StartsWith("Queued Hold:", feedbackLine.text);

            // Advance sim past C2 delay so expanded Hold orders apply to both group members
            for (var t = 2; t <= 4; t++)
            {
                world.SimTime = t;
                bridgeHost.RunTick(wrapped, world);
                yield return null;
                if (world.Applied.Skip(appliedBeforeSubmit).Count(a => a.Order.Kind == OrderKind.Hold) >= 2)
                {
                    break;
                }
            }

            var postSubmitHolds = world.Applied.Skip(appliedBeforeSubmit).Where(a => a.Order.Kind == OrderKind.Hold).ToArray();
            Assert.AreEqual(2, postSubmitHolds.Length);
            Assert.AreEqual(2, postSubmitHolds.Select(a => a.Entity).Distinct().Count());
        }

        [UnityTest]
        public IEnumerator CoordinationPanelHost_explains_blocked_reattack_and_missing_member_with_zero_order_log_mutation()
        {
            _go = new GameObject("sprint3-coord-blocked-test");
            var bridgeHost = _go.AddComponent<DelegationBridgeHost>();
            var document = CreateDocument(_go);
            yield return null;

            Assert.IsTrue(PlayModeSmokeOrbatSeeder.TrySeedWithEscortTaskGroup(bridgeHost.Bridge));
            bridgeHost.BeginExecution();

            var alive = new Dictionary<string, bool>
            {
                [PlayModeSmokeOrbatSeeder.FriendlyUnitId] = true,
                [PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId] = true,
                [PlayModeSmokeOrbatSeeder.HostileUnitId] = true,
            };
            var world = new StubWorld(alive) { SimTime = 1.0 };
            var wrapped = PlayModeSmokeCoordinationFactsProvider.WrapSnapshot(world);
            bridgeHost.RunTick(wrapped, world);
            yield return null;

            var panelHost = _go.AddComponent<CoordinationPanelHost>();
            panelHost.enabled = false;
            BuildCoordinationTree(
                document.rootVisualElement,
                out _,
                out var blockReasonLine,
                out var holdBtn,
                out var withdrawBtn,
                out var reattackBtn,
                out var feedbackLine);
            SetPrivateField(panelHost, "bridgeHost", bridgeHost);
            panelHost.enabled = true;
            yield return null;

            var playerOrdersBefore = bridgeHost.Bridge.Orchestrator.DecisionLog.PlayerOrders.Count;
            SendSubmit(reattackBtn);
            yield return null;

            Assert.AreEqual(playerOrdersBefore, bridgeHost.Bridge.Orchestrator.DecisionLog.PlayerOrders.Count);
            StringAssert.Contains("advisory", feedbackLine.text.ToLowerInvariant());

            // Mark escort dead -> atomic group Hold/Withdraw blocked by MISSING_OR_LOST_MEMBER
            alive[PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId] = false;
            world.SimTime = 2.0;
            bridgeHost.RunTick(wrapped, world);
            yield return null;

            Assert.IsFalse(holdBtn.enabledSelf);
            Assert.IsFalse(withdrawBtn.enabledSelf);
            StringAssert.Contains("detached", blockReasonLine.text.ToLowerInvariant());
            Assert.AreEqual(playerOrdersBefore, bridgeHost.Bridge.Orchestrator.DecisionLog.PlayerOrders.Count);
        }

        [UnityTest]
        public IEnumerator CoordinationPanelHost_disables_group_commands_and_explains_replay_and_timeline_inspection()
        {
            _go = new GameObject("sprint3-coord-replay-timeline-test");
            var bridgeHost = _go.AddComponent<DelegationBridgeHost>();
            var document = CreateDocument(_go);
            yield return null;

            Assert.IsTrue(PlayModeSmokeOrbatSeeder.TrySeedWithEscortTaskGroup(bridgeHost.Bridge));
            bridgeHost.BeginExecution();

            var alive = new Dictionary<string, bool>
            {
                [PlayModeSmokeOrbatSeeder.FriendlyUnitId] = true,
                [PlayModeSmokeOrbatSeeder.FriendlyEscortUnitId] = true,
                [PlayModeSmokeOrbatSeeder.HostileUnitId] = true,
            };
            var world = new StubWorld(alive) { SimTime = 1.0 };
            var wrapped = PlayModeSmokeCoordinationFactsProvider.WrapSnapshot(world);
            bridgeHost.RunTick(wrapped, world);
            yield return null;

            var panelHost = _go.AddComponent<CoordinationPanelHost>();
            panelHost.enabled = false;
            BuildCoordinationTree(
                document.rootVisualElement,
                out _,
                out var blockReasonLine,
                out var holdBtn,
                out var withdrawBtn,
                out _,
                out _);
            SetPrivateField(panelHost, "bridgeHost", bridgeHost);
            panelHost.enabled = true;
            yield return null;

            // 1. Replay viewer attached
            bridgeHost.Bridge.AttachReplayViewer = true;
            yield return null;
            Assert.IsFalse(holdBtn.enabledSelf);
            Assert.IsFalse(withdrawBtn.enabledSelf);
            StringAssert.Contains("replay", blockReasonLine.text.ToLowerInvariant());

            // 2. Timeline inspection active
            bridgeHost.Bridge.AttachReplayViewer = false;
            var timelineRows = new[]
            {
                new CombatEvent(
                    CombatEventPhase.Firing,
                    PlayModeSmokeOrbatSeeder.FriendlyUnitId,
                    PlayModeSmokeOrbatSeeder.HostileUnitId,
                    "Gun",
                    "Launch",
                    1UL,
                    1.0,
                    1UL,
                    "Test timeline entry"),
            };
            bridgeHost.CommandTimeline.Capture(
                CombatPresentationFrame.Empty with
                {
                    SimTime = 1.0,
                    Events = new CombatEventSnapshot(timelineRows),
                },
                Array.Empty<MapSymbolEntry>());
            var entry = bridgeHost.CommandTimeline.Filter(new AfterActionLedgerFilter())[0];
            Assert.IsTrue(bridgeHost.InspectTimelineEvent(entry));
            yield return null;

            Assert.IsFalse(holdBtn.enabledSelf);
            Assert.IsFalse(withdrawBtn.enabledSelf);
            StringAssert.Contains("timeline", blockReasonLine.text.ToLowerInvariant());
        }

        private static void SendSubmit(VisualElement target)
        {
            using var submit = NavigationSubmitEvent.GetPooled();
            submit.target = target;
            target.SendEvent(submit);
        }

        private static void BuildCoordinationTree(
            VisualElement docRoot,
            out VisualElement boundariesList,
            out Label blockReasonLine,
            out Button holdButton,
            out Button withdrawButton,
            out Button reattackButton,
            out Label feedbackLine)
        {
            var root = new VisualElement { name = "coordination-panel-root" };
            root.Add(new Label { name = "coord-header-title" });
            root.Add(new Label { name = "coord-advisory-badge" });
            root.Add(new DropdownField { name = "coord-group-dropdown" });
            root.Add(new Label { name = "coord-intent-line" });
            root.Add(new Label { name = "coord-controller-line" });
            boundariesList = new VisualElement { name = "coord-boundaries-list" };
            root.Add(boundariesList);
            root.Add(new VisualElement { name = "coord-effects-list" });
            blockReasonLine = new Label { name = "coord-block-reason-line" };
            root.Add(blockReasonLine);
            holdButton = new Button { name = "coord-hold-button", text = "Hold" };
            withdrawButton = new Button { name = "coord-withdraw-button", text = "Withdraw" };
            reattackButton = new Button { name = "coord-reattack-button", text = "Reattack (Adv)" };
            root.Add(holdButton);
            root.Add(withdrawButton);
            root.Add(reattackButton);

            var confirmBox = new VisualElement { name = "coord-assisted-confirm-box" };
            confirmBox.Add(new Label { name = "coord-assisted-confirm-title" });
            confirmBox.Add(new Label { name = "coord-assisted-confirm-body" });
            confirmBox.Add(new Button { name = "coord-assisted-confirm-accept" });
            confirmBox.Add(new Button { name = "coord-assisted-confirm-cancel" });
            root.Add(confirmBox);

            feedbackLine = new Label { name = "coord-feedback-line" };
            root.Add(feedbackLine);
            docRoot.Add(root);
        }

        private static void SetPrivateField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
    }
}
#endif
