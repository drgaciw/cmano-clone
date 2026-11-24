// Sprint 2 DRG-245 PlayMode verification: Dual-side TEST SANDBOX banner in C2TopBarPanelHost.
#if UNITY_5_3_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
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
    public sealed class Sprint2TopBarSandboxPlayModeTests
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
        public IEnumerator Top_bar_shows_test_sandbox_banner_iff_dual_side_policy_and_mixed_mode()
        {
            var go = new GameObject("sprint2-topbar-sandbox-test");
            _cleanup.Add(go);
            go.SetActive(false);

            var bridgeHost = go.AddComponent<DelegationBridgeHost>();
            SetPrivateField(bridgeHost, "scenarioPolicyId", "test-sandbox-dual-side");
            var document = CreateDocument(go);
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
            BuildTopBarTree(document.rootVisualElement, out var sandboxBanner);
            SetPrivateField(topBar, "bridgeHost", bridgeHost);
            topBar.enabled = true;
            yield return null;

            Assert.AreEqual(DisplayStyle.Flex, sandboxBanner.style.display.value);
            StringAssert.Contains("TEST SANDBOX", sandboxBanner.text);
            Assert.IsTrue(topBar.LastLoopPolicyChrome.ShowDualSideTestSandboxBanner);

            // Switch to Human mode -> banner must hide immediately.
            bridgeHost.Bridge.ConfigureSimulationMode(
                new SimulationModeProfile(SimulationModeKind.Human, PlayerControlsFriendlySide: true),
                new[] { friendly.Target },
                new[] { opposing.Target },
                PersonalityCatalog.All[0].Traits);
            yield return null;

            Assert.AreEqual(DisplayStyle.None, sandboxBanner.style.display.value);
            Assert.IsFalse(topBar.LastLoopPolicyChrome.ShowDualSideTestSandboxBanner);
        }

        [UnityTest]
        public IEnumerator Top_bar_hides_test_sandbox_banner_for_standard_baltic_mixed_mode()
        {
            var go = new GameObject("sprint2-topbar-standard-baltic-test");
            _cleanup.Add(go);

            var bridgeHost = go.AddComponent<DelegationBridgeHost>();
            var document = CreateDocument(go);
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
            BuildTopBarTree(document.rootVisualElement, out var sandboxBanner);
            SetPrivateField(topBar, "bridgeHost", bridgeHost);
            topBar.enabled = true;
            yield return null;

            Assert.AreEqual(DisplayStyle.None, sandboxBanner.style.display.value);
            Assert.IsFalse(topBar.LastLoopPolicyChrome.ShowDualSideTestSandboxBanner);
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

        private static void BuildTopBarTree(VisualElement documentRoot, out Label sandboxBanner)
        {
            var root = new VisualElement { name = "c2-topbar-root" };
            sandboxBanner = new Label { name = LoopPolicyChromeBinder.TestSandboxBannerElementName };
            root.Add(sandboxBanner);
            root.Add(new Label { name = "sim-time-label" });
            root.Add(new Label { name = "phase-label" });
            root.Add(new Label { name = "compression-label" });
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
