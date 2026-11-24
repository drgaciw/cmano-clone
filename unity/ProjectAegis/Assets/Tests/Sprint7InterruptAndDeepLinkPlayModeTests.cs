// Sprint 7 PlayMode verification (DRG-269 Pending-Approval 1x Interrupt + DLZ Countdown; DRG-268 Message-Log -> Unit + EngageExplain Deep-Link).
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
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Tests
{
    public sealed class Sprint7InterruptAndDeepLinkPlayModeTests
    {
        private sealed class StubWorld : ISimWorldSnapshot, IOrderSink
        {
            public double SimTime { get; set; } = 10.0;
            public int ContactCount => 1;
            public int ActiveEngagementCount => 0;
            public TargetId? PrimaryHostileContactId => new TargetId(SimplePlayModeSimHost.SmokeHostileUnitId);
            public bool HasFireControlTrackOnPrimaryContact => true;
            public bool ObserverRadarEmconActive => true;
            public bool IsMemberAlive(TargetId memberId) => true;
            public void ApplyOrder(EntityKey entity, in Order order) { }
        }

        private GameObject? _go;
        private readonly List<UnityEngine.Object> _cleanup = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
            for (var i = 0; i < _cleanup.Count; i++)
            {
                if (_cleanup[i] != null) UnityEngine.Object.DestroyImmediate(_cleanup[i]);
            }

            _cleanup.Clear();
            _go = null;
        }

        private UIDocument CreateDocument(GameObject host, string uxmlAssetPath)
        {
            var doc = host.AddComponent<UIDocument>();
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            var theme = ScriptableObject.CreateInstance<ThemeStyleSheet>();
            settings.themeStyleSheet = theme;
            doc.panelSettings = settings;
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlAssetPath);
            Assert.IsNotNull(tree, $"Expected VisualTreeAsset at {uxmlAssetPath}");
            doc.visualTreeAsset = tree;
            _cleanup.Add(settings);
            _cleanup.Add(theme);
            return doc;
        }

        private static void SetPrivateField(object target, string fieldName, object? value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            field!.SetValue(target, value);
        }

        [UnityTest]
        public IEnumerator Drg269_PendingApprovalInterrupt_DropsAcceleratedClockTo1xOnce_AndShowsSimTimeDlzCountdown()
        {
            _go = new GameObject("sprint7-drg269-interrupt-test");
            var bridge = _go.AddComponent<DelegationBridgeHost>();
            var doc = CreateDocument(_go, "Assets/UI/PendingApproval/PendingApprovalPanel.uxml");
            yield return null;

            Assert.IsTrue(SimplePlayModeSimHost.TrySeedSmokeOrbat(bridge.Bridge));
            bridge.BeginExecution();
            var world = new StubWorld { SimTime = 10.0 };
            bridge.RunTick(world, world);

            // Enqueue a pending-approval order and set authoritative DLZ expiry at sim time 40s (30s remaining)
            var orderId = new OrderId(7001);
            bridge.Bridge.Orchestrator.PendingApprovalQueue.Enqueue(
                new Order(orderId, new TargetId(SimplePlayModeSimHost.SmokeHostileUnitId), 10.0, OrderKind.Engage, RiskLevel.High));
            bridge.SetPendingApprovalDlzExpiry(orderId, 40.0);

            var panel = _go.AddComponent<PendingApprovalPanelHost>();
            panel.enabled = false;
            SetPrivateField(panel, "bridgeHost", bridge);
            panel.enabled = true;
            yield return null;

            var interruptLabel = doc.rootVisualElement.Q<Label>("pending-approval-interrupt-label");
            var dlzLabel = doc.rootVisualElement.Q<Label>("pending-approval-dlz-countdown");
            Assert.IsNotNull(interruptLabel);
            Assert.IsNotNull(dlzLabel);
            Assert.That(dlzLabel!.text, Does.Contain("30.0s"));

            // Second evaluation at same sim time (paused): countdown halts at 30.0s and does NOT re-issue 1x drop
            var batch2 = bridge.EvaluatePendingApprovalInterrupts();
            Assert.IsFalse(batch2.ShouldDropToRealTime);
            Assert.AreEqual(1, batch2.Cards.Count);
            Assert.That(batch2.Cards[0].RemainingSimSeconds, Is.EqualTo(30.0).Within(0.001));

            // Approve through validated command boundary
            Assert.IsTrue(bridge.TryApprovePendingOrderValidated(orderId, out var validation));
            Assert.AreEqual(PendingApprovalInterruptCoordinator.ReasonOk, validation.ReasonCode);
        }

        [UnityTest]
        public IEnumerator Drg269_MissingDlzDataShowsUnknown_AndExpiredApprovalIsRejected()
        {
            _go = new GameObject("sprint7-drg269-unknown-expired-test");
            var bridge = _go.AddComponent<DelegationBridgeHost>();
            var doc = CreateDocument(_go, "Assets/UI/PendingApproval/PendingApprovalPanel.uxml");
            yield return null;

            Assert.IsTrue(SimplePlayModeSimHost.TrySeedSmokeOrbat(bridge.Bridge));
            bridge.BeginExecution();
            var world = new StubWorld { SimTime = 25.0 };
            bridge.RunTick(world, world);

            var orderUnknown = new OrderId(7002);
            bridge.Bridge.Orchestrator.PendingApprovalQueue.Enqueue(
                new Order(orderUnknown, new TargetId(SimplePlayModeSimHost.SmokeHostileUnitId), 25.0, OrderKind.Engage, RiskLevel.High));

            var panel = _go.AddComponent<PendingApprovalPanelHost>();
            panel.enabled = false;
            SetPrivateField(panel, "bridgeHost", bridge);
            panel.enabled = true;
            yield return null;

            var dlzLabel = doc.rootVisualElement.Q<Label>("pending-approval-dlz-countdown");
            Assert.IsNotNull(dlzLabel);
            Assert.That(dlzLabel!.text, Does.Contain("UNKNOWN"));

            // Now bind an already-expired DLZ window (20.0 <= 25.0) and verify approval is blocked fail-closed
            bridge.SetPendingApprovalDlzExpiry(orderUnknown, 20.0);
            Assert.IsFalse(bridge.TryApprovePendingOrderValidated(orderUnknown, out var expiredVal));
            Assert.AreEqual(PendingApprovalInterruptCoordinator.ReasonDlzExpired, expiredVal.ReasonCode);
        }

        [UnityTest]
        public IEnumerator Drg268_MessageLogDeepLink_SelectsVisibleUnit_AndOpensEngageExplainSurface()
        {
            _go = new GameObject("sprint7-drg268-deeplink-explain-test");
            var bridge = _go.AddComponent<DelegationBridgeHost>();
            var explainDoc = CreateDocument(_go, "Assets/UI/EngageExplain/EngageExplainPanel.uxml");
            yield return null;

            Assert.IsTrue(SimplePlayModeSimHost.TrySeedSmokeOrbat(bridge.Bridge));
            bridge.BeginExecution();
            var world = new StubWorld { SimTime = 5.0 };
            bridge.RunTick(world, world);

            var explainHost = _go.AddComponent<EngageExplainPanelHost>();
            explainHost.enabled = false;
            SetPrivateField(explainHost, "bridgeHost", bridge);
            explainHost.enabled = true;
            yield return null;

            var detail = new DecisionLogDeepLinkDetail(
                Found: true,
                SequenceId: 77,
                SimTick: 5,
                SimTime: 5.0,
                EntryKind: OrderLogEntryKind.PolicyDenial,
                Category: "ENGAGE",
                SourceKind: "LiveOrderLogView",
                ActorId: SimplePlayModeSimHost.SmokeFriendlyUnitId,
                AgentId: "agent-1",
                UnitId: SimplePlayModeSimHost.SmokeFriendlyUnitId,
                Summary: "Hold fire under ROE",
                Rationale: "ROE_HOLD_FIRE",
                AutonomyLevel: "Assisted",
                AttentionLoad: 0.2,
                AttentionBudget: 1.0,
                RngDraw: 0.1,
                PolicySnapshotId: 1,
                ScoredAlternatives: System.Array.Empty<string>(),
                Fields: new[] { new KeyValuePair<string, string>("TargetId", SimplePlayModeSimHost.SmokeHostileUnitId) },
                UnavailableReason: null);

            var resolved = bridge.SelectMessageLogDeepLink(detail);
            yield return null;

            Assert.IsTrue(resolved.HasExplain);
            Assert.IsFalse(resolved.SuppressedByFog);
            Assert.AreEqual(SimplePlayModeSimHost.SmokeFriendlyUnitId, bridge.SelectedUnitId);
            Assert.That(explainHost.LastExplain.StatusLine, Does.Contain("SEQ #77"));
            var statusLabel = explainDoc.rootVisualElement.Q<Label>("engage-explain-status");
            Assert.IsNotNull(statusLabel);
            Assert.That(statusLabel!.text, Does.Contain("SEQ #77"));
        }

        [UnityTest]
        public IEnumerator Drg268_MessageLogDeepLink_SuppressesUnobservedHostileUnderFog()
        {
            _go = new GameObject("sprint7-drg268-fog-test");
            var bridge = _go.AddComponent<DelegationBridgeHost>();
            yield return null;

            Assert.IsTrue(SimplePlayModeSimHost.TrySeedSmokeOrbat(bridge.Bridge));
            bridge.SelectUnit(SimplePlayModeSimHost.SmokeFriendlyUnitId);

            var hiddenDetail = new DecisionLogDeepLinkDetail(
                Found: true,
                SequenceId: 88,
                SimTick: 6,
                SimTime: 6.0,
                EntryKind: OrderLogEntryKind.AgentDecision,
                Category: "CONTACT",
                SourceKind: "LiveOrderLogView",
                ActorId: "unobserved-enemy-999",
                AgentId: "red-agent",
                UnitId: "unobserved-enemy-999",
                Summary: "Unobserved enemy unit",
                Rationale: null,
                AutonomyLevel: null,
                AttentionLoad: null,
                AttentionBudget: null,
                RngDraw: null,
                PolicySnapshotId: null,
                ScoredAlternatives: System.Array.Empty<string>(),
                Fields: System.Array.Empty<KeyValuePair<string, string>>(),
                UnavailableReason: null);

            var resolved = bridge.SelectMessageLogDeepLink(hiddenDetail);
            Assert.IsTrue(resolved.SuppressedByFog);
            Assert.IsNull(resolved.SafeUnitIdToSelect);
            Assert.AreEqual(SimplePlayModeSimHost.SmokeFriendlyUnitId, bridge.SelectedUnitId);
            Assert.That(resolved.Explain.StatusLine, Does.Contain("FOG"));
        }
    }
}
#endif
