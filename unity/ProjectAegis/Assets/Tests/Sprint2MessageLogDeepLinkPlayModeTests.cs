// Sprint 2 DRG-273 PlayMode verification: Attributed message log + non-mutating DecisionLog deep-link drawer.
#if UNITY_5_3_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Unity.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Tests
{
    public sealed class Sprint2MessageLogDeepLinkPlayModeTests
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
        public IEnumerator Selecting_attributed_row_populates_non_mutating_DecisionLog_deeplink_drawer()
        {
            var go = new GameObject("sprint2-deeplink-playmode-test");
            _cleanup.Add(go);

            var bridgeHost = go.AddComponent<DelegationBridgeHost>();
            var doc = CreateDocument(go);
            yield return null;

            bridgeHost.Bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
            var log = bridgeHost.Bridge.Orchestrator.DecisionLog;
            log.Append(new DecisionRecord(
                2.0, new AgentId("agent-baltic"), new TargetId("u1"), AutonomyLevel.Assisted,
                OrderKind.Hold, System.Array.Empty<ScoredIntent>(), "station keeping", 2, 10, 0.3, SimTick: 2));

            bridgeHost.BeginExecution();
            var world = new StubWorld { SimTime = 2.0 };
            bridgeHost.RunTick(world, world);

            var logPanel = go.AddComponent<MessageLogPanelHost>();
            logPanel.enabled = false;
            BuildMessageLogTree(doc.rootVisualElement, out var drawer, out var header, out var attribution, out var detail);
            SetPrivateField(logPanel, "bridgeHost", bridgeHost);
            logPanel.enabled = true;
            yield return null;

            var fingerprintBefore = log.ComputeFingerprint();
            var entryCountBefore = log.ChronologicalEntries().Count;

            Assert.IsTrue(logPanel.TrySelectRow(0));
            yield return null;

            Assert.AreEqual(DisplayStyle.Flex, drawer.style.display.value);
            StringAssert.Contains("AGENT_DECISION", header.text);
            StringAssert.Contains("agent-baltic", attribution.text);
            StringAssert.Contains("station keeping", detail.text);
            Assert.AreEqual(fingerprintBefore, log.ComputeFingerprint());
            Assert.AreEqual(entryCountBefore, log.ChronologicalEntries().Count);
            Assert.IsNull(bridgeHost.DisplayCombatKey);
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

        private static void BuildMessageLogTree(
            VisualElement documentRoot,
            out VisualElement drawer,
            out Label header,
            out Label attribution,
            out Label detail)
        {
            var root = new VisualElement { name = "message-log-root" };
            root.Add(new Label("MESSAGE LOG") { name = "message-log-title" });
            root.Add(new Label { name = "message-log-onboarding-hint" });
            root.Add(new ListView { name = "message-list" });
            drawer = new VisualElement { name = "message-log-deeplink-drawer" };
            header = new Label { name = "message-log-deeplink-header" };
            attribution = new Label { name = "message-log-deeplink-attribution" };
            detail = new Label { name = "message-log-deeplink-detail" };
            drawer.Add(header);
            drawer.Add(attribution);
            drawer.Add(detail);
            root.Add(drawer);
            documentRoot.Add(root);
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
