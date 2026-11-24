// Sprint 2 DRG-398 PlayMode verification: Time compression safe-stop (1x/4x/8x parity)
// & C2TopBarPanelHost observer/notification pause gating preserving M3 test-sandbox-banner.
#if UNITY_5_3_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Unity.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Tests
{
    public sealed class Sprint2TimeCompressionSafeStopPlayModeTests
    {
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
        public IEnumerator SimplePlayModeSimHost_safe_stops_at_8x_on_live_damage_event()
        {
            var go = new GameObject("sprint2-safe-stop-simhost-test");
            _cleanup.Add(go);
            go.SetActive(false);

            var bridgeHost = go.AddComponent<DelegationBridgeHost>();
            var simHost = go.AddComponent<SimplePlayModeSimHost>();
            SetPrivateField(simHost, "bridgeHost", bridgeHost);
            SetPrivateField(simHost, "autoBeginOnStart", false);
            SetPrivateField(simHost, "seedSmokeOrbatOnStart", true);
            go.SetActive(true);
            yield return null;

            bridgeHost.BeginExecution();
            Assert.IsTrue(bridgeHost.TrySetTimeAcceleration(8, out _));
            Assert.AreEqual(8, bridgeHost.Session!.TimeAccelerationFactor);

            // Trigger t=20s seeder DAMAGE row during a single RunStep-style step.
            SetPrivateField(simHost, "_simTime", 20.0);
            var runStep = typeof(SimplePlayModeSimHost).GetMethod("RunStep", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(runStep);
            runStep!.Invoke(simHost, null);
            yield return null;

            Assert.IsTrue(bridgeHost.HasUnresolvedNotificationPause);
            Assert.IsTrue(bridgeHost.Session.IsSimPaused);
            Assert.AreEqual(1, bridgeHost.Session.TimeAccelerationFactor);
            Assert.AreEqual("TIME: PAUSED (1x)", bridgeHost.LiveCompressionLabel);
        }

        [UnityTest]
        public IEnumerator TopBar_preserves_sandbox_banner_and_disables_clock_under_AttachReplayViewer()
        {
            var go = new GameObject("sprint2-topbar-safestop-observer-test");
            _cleanup.Add(go);
            go.SetActive(false);

            var bridgeHost = go.AddComponent<DelegationBridgeHost>();
            SetPrivateField(bridgeHost, "scenarioPolicyId", "test-sandbox-dual-side");
            var doc = CreateDocument(go);
            go.SetActive(true);
            yield return null;

            var friendly = bridgeHost.Bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
            var opposing = bridgeHost.Bridge.Registry.RegisterUnit(new EntityKey(2), "hostile-1");
            bridgeHost.Bridge.ConfigureSimulationMode(
                new SimulationModeProfile(SimulationModeKind.Mixed, PlayerControlsFriendlySide: true),
                new[] { friendly.Target },
                new[] { opposing.Target },
                PersonalityCatalog.All[0].Traits);

            var topBar = go.AddComponent<C2TopBarPanelHost>();
            topBar.enabled = false;
            BuildTopBarTree(doc.rootVisualElement, out var sandboxBanner, out var slower, out var faster, out var pauseResume);
            SetPrivateField(topBar, "bridgeHost", bridgeHost);
            topBar.enabled = true;
            yield return null;

            Assert.AreEqual(DisplayStyle.Flex, sandboxBanner.style.display.value);
            Assert.IsTrue(slower.enabledSelf);
            Assert.IsTrue(faster.enabledSelf);
            Assert.IsTrue(pauseResume.enabledSelf);

            bridgeHost.Bridge.AttachReplayViewer = true;
            yield return null;

            Assert.IsFalse(slower.enabledSelf);
            Assert.IsFalse(faster.enabledSelf);
            Assert.IsFalse(pauseResume.enabledSelf);
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

        private static void BuildTopBarTree(
            VisualElement documentRoot,
            out Label sandboxBanner,
            out Button slower,
            out Button faster,
            out Button pauseResume)
        {
            var root = new VisualElement { name = "c2-topbar-root" };
            sandboxBanner = new Label { name = LoopPolicyChromeBinder.TestSandboxBannerElementName };
            slower = new Button { name = "compression-slower-button" };
            faster = new Button { name = "compression-faster-button" };
            pauseResume = new Button { name = "pause-resume-button" };
            root.Add(sandboxBanner);
            root.Add(new Label { name = "sim-time-label" });
            root.Add(new Label { name = "phase-label" });
            root.Add(new Label { name = "compression-label" });
            root.Add(slower);
            root.Add(faster);
            root.Add(pauseResume);
            root.Add(new Label { name = "mode-label" });
            root.Add(new Label { name = "fog-mode-label" });
            root.Add(new Label { name = "edit-policy-label" });
            root.Add(new Label { name = "score-label" });
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
