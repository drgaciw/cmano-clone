#if UNITY_5_3_OR_NEWER
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Unity.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Tests
{
    public sealed class ContactOverridePlayModeTests
    {
        private sealed class StubWorld : ISimWorldSnapshot, IOrderSink
        {
            public double SimTime { get; set; } = 2.0;
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
        public IEnumerator Reclassify_button_updates_override_status_and_map_symbol_via_bridge_host()
        {
            _go = new GameObject("sprint2-contact-reclassify-test");
            var bridgeHost = _go.AddComponent<DelegationBridgeHost>();
            var document = CreateDocument(_go);
            yield return null;

            Assert.IsTrue(SimplePlayModeSimHost.TrySeedSmokeOrbat(bridgeHost.Bridge));
            bridgeHost.BeginExecution();
            var world = new StubWorld();
            bridgeHost.RunTick(world, world);
            bridgeHost.SelectContact(SimplePlayModeSimHost.SmokeContactId);
            yield return null;

            var panelHost = _go.AddComponent<ContactDetailPanelHost>();
            panelHost.enabled = false;
            BuildContactPanelTree(
                document.rootVisualElement,
                out var statusLine,
                out var dropdown,
                out var reclassifyBtn,
                out _,
                out _);
            SetPrivateField(panelHost, "bridgeHost", bridgeHost);
            panelHost.enabled = true;
            yield return null;

            dropdown.value = "Neutral";
            SendSubmit(reclassifyBtn);
            yield return null;

            StringAssert.Contains("RECLASSIFIED → Neutral", statusLine.text);
            Assert.AreEqual(1, bridgeHost.Bridge.Orchestrator.DecisionLog.ContactOverrides.Count);
            Assert.AreEqual(ContactOverrideAction.Reclassify,
                bridgeHost.Bridge.Orchestrator.DecisionLog.ContactOverrides[0].Action);
            var symbol = bridgeHost.LastMapSymbols.Single(s => s.SymbolId == SimplePlayModeSimHost.SmokeContactId);
            Assert.AreEqual("Neutral", symbol.Affiliation);
        }

        [UnityTest]
        public IEnumerator Drop_button_marks_contact_dropped_even_while_sim_paused()
        {
            _go = new GameObject("sprint2-contact-drop-paused-test");
            var bridgeHost = _go.AddComponent<DelegationBridgeHost>();
            var document = CreateDocument(_go);
            yield return null;

            Assert.IsTrue(SimplePlayModeSimHost.TrySeedSmokeOrbat(bridgeHost.Bridge));
            bridgeHost.BeginExecution();
            var world = new StubWorld();
            bridgeHost.RunTick(world, world);
            bridgeHost.SelectContact(SimplePlayModeSimHost.SmokeContactId);
            Assert.IsTrue(bridgeHost.TryPauseSim(out var pauseReason), pauseReason);
            yield return null;

            var panelHost = _go.AddComponent<ContactDetailPanelHost>();
            panelHost.enabled = false;
            BuildContactPanelTree(
                document.rootVisualElement,
                out var statusLine,
                out _,
                out _,
                out var dropBtn,
                out _);
            SetPrivateField(panelHost, "bridgeHost", bridgeHost);
            panelHost.enabled = true;
            yield return null;

            var contactChangesBefore = bridgeHost.Bridge.Orchestrator.DecisionLog.ContactChanges.Count;
            SendSubmit(dropBtn);
            yield return null;

            StringAssert.Contains("DROPPED", statusLine.text);
            Assert.IsFalse(dropBtn.enabledSelf);
            Assert.IsFalse(bridgeHost.LastMapSymbols.Any(s => s.SymbolId == SimplePlayModeSimHost.SmokeContactId));
            Assert.IsFalse(bridgeHost.LastSensorC2.Contacts.Any(c => c.ContactId == SimplePlayModeSimHost.SmokeContactId));
            Assert.AreEqual(contactChangesBefore, bridgeHost.Bridge.Orchestrator.DecisionLog.ContactChanges.Count);
        }

        [UnityTest]
        public IEnumerator AttachReplayViewer_disables_override_controls_and_shows_reason_with_zero_log_writes()
        {
            _go = new GameObject("sprint2-contact-replay-test");
            var bridgeHost = _go.AddComponent<DelegationBridgeHost>();
            var document = CreateDocument(_go);
            yield return null;

            Assert.IsTrue(SimplePlayModeSimHost.TrySeedSmokeOrbat(bridgeHost.Bridge));
            bridgeHost.BeginExecution();
            var world = new StubWorld();
            bridgeHost.RunTick(world, world);
            bridgeHost.SelectContact(SimplePlayModeSimHost.SmokeContactId);
            yield return null;

            var panelHost = _go.AddComponent<ContactDetailPanelHost>();
            panelHost.enabled = false;
            BuildContactPanelTree(
                document.rootVisualElement,
                out _,
                out var dropdown,
                out var reclassifyBtn,
                out var dropBtn,
                out var feedbackLine);
            SetPrivateField(panelHost, "bridgeHost", bridgeHost);
            panelHost.enabled = true;
            yield return null;

            bridgeHost.Bridge.AttachReplayViewer = true;
            var overridesBefore = bridgeHost.Bridge.Orchestrator.DecisionLog.ContactOverrides.Count;
            yield return null;

            Assert.IsFalse(dropdown.enabledSelf);
            Assert.IsFalse(reclassifyBtn.enabledSelf);
            Assert.IsFalse(dropBtn.enabledSelf);
            StringAssert.Contains("replay viewer attached", feedbackLine.text.ToLowerInvariant());

            Assert.IsFalse(bridgeHost.TryReclassifySelectedContact("Friendly", out _));
            Assert.IsFalse(bridgeHost.TryDropSelectedContact(out _));
            Assert.AreEqual(overridesBefore, bridgeHost.Bridge.Orchestrator.DecisionLog.ContactOverrides.Count);
        }

        private static void SendSubmit(VisualElement target)
        {
            using var submit = NavigationSubmitEvent.GetPooled();
            submit.target = target;
            target.SendEvent(submit);
        }

        private static void BuildContactPanelTree(
            VisualElement documentRoot,
            out Label statusLine,
            out DropdownField dropdown,
            out Button reclassifyBtn,
            out Button dropBtn,
            out Label feedbackLine)
        {
            var root = new VisualElement { name = "contact-detail-root" };
            root.Add(new Label { name = "contact-id-line" });
            root.Add(new Label { name = "target-id-line" });
            root.Add(new Label { name = "classification-line" });
            statusLine = new Label { name = "override-status-line" };
            dropdown = new DropdownField { name = "contact-reclassify-dropdown" };
            reclassifyBtn = new Button { name = "contact-reclassify-button", text = "Apply Reclassify" };
            dropBtn = new Button { name = "contact-drop-button", text = "Drop Contact" };
            feedbackLine = new Label { name = "override-feedback-line" };
            root.Add(statusLine);
            root.Add(dropdown);
            root.Add(reclassifyBtn);
            root.Add(dropBtn);
            root.Add(feedbackLine);
            documentRoot.Add(root);
        }

        private static void SetPrivateField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
    }
}
#endif
