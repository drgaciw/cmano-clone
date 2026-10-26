#if UNITY_5_3_OR_NEWER
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Unity.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Tests
{
    /// <summary>Last-mile bind/cache/selection checks; does not certify a combat scenario.</summary>
    public sealed class SliceAContactPanelSmokeTests
    {
        [UnityTest]
        public IEnumerator Jev_attention_rebinds_after_override_document_rebuild()
        {
            var view = new GameObject("jev-attention-host-test");
            var externalView = new GameObject("jev-attention-document-test");
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            var theme = ScriptableObject.CreateInstance<ThemeStyleSheet>();
            settings.themeStyleSheet = theme;
            view.SetActive(false);
            try
            {
                var document = externalView.AddComponent<UIDocument>();
                document.panelSettings = settings;
                var host = view.AddComponent<JevAttentionPanelHost>();
                typeof(JevAttentionPanelHost).GetField("documentOverride", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(host, document);
                view.SetActive(true);
                yield return null;

                VisualElement AddPanel()
                {
                    var root = new VisualElement { name = "jev-attention-root" };
                    root.Add(new Label { name = "jev-model-pin" });
                    root.Add(new Label { name = "jev-disclosure-banner" });
                    root.Add(new VisualElement { name = "jev-attention-list" });
                    document.rootVisualElement.Add(root);
                    return root;
                }

                const string available = "{\"schema\":\"aegis.jev.state.v0\",\"unitId\":\"u-rebind\",\"skillAvailability\":{\"jevDecide\":true}}";
                const string offline = "{\"schema\":\"aegis.jev.state.v0\",\"unitId\":\"u-rebind\",\"skillAvailability\":{\"jevDecide\":false}}";
                var original = AddPanel();
                host.ApplyStateJson(available);
                Assert.That(original.Q<Label>("jev-disclosure-banner").text, Does.Contain("LOCAL HEURISTIC"));

                document.enabled = false;
                host.ApplyStateJson(offline);
                Assert.That(host.LastPanel!.Disclosure.Visible, Is.True);
                Assert.That(original.Q<Label>("jev-disclosure-banner").text, Does.Contain("LOCAL HEURISTIC"),
                    "An unavailable document must not paint detached elements.");
                document.enabled = true;
                yield return null;

                var rebuilt = AddPanel();
                host.Apply(host.LastPanel!);
                Assert.That(rebuilt.Q<Label>("jev-disclosure-banner").text, Does.Contain("ASSIST UNAVAILABLE"));
                Assert.That(rebuilt.Q<Label>("jev-model-pin").text, Is.EqualTo("SOURCE: UNAVAILABLE"));
                Assert.That(rebuilt.Q("jev-attention-list").childCount, Is.EqualTo(1));

                document.rootVisualElement.Clear();
                var replaced = AddPanel();
                host.ApplyStateJson(available);
                Assert.That(replaced.Q<Label>("jev-disclosure-banner").text, Does.Contain("LOCAL HEURISTIC"));
                Assert.That(replaced.Q<Label>("jev-model-pin").text, Is.EqualTo("SOURCE: LOCAL HEURISTIC"));
                Assert.That(rebuilt.Q<Label>("jev-disclosure-banner").text, Does.Contain("ASSIST UNAVAILABLE"));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                Object.Destroy(view);
                Object.Destroy(externalView);
                Object.Destroy(settings);
                Object.Destroy(theme);
            }
        }

        [UnityTest]
        public IEnumerator Top_bar_enable_before_bridge_awake_waits_then_binds_live_clock()
        {
            var composition = new GameObject("top-bar-composition-test");
            var view = new GameObject("top-bar-lifecycle-test");
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            var theme = ScriptableObject.CreateInstance<ThemeStyleSheet>();
            settings.themeStyleSheet = theme;
            composition.SetActive(false);
            try
            {
                var host = composition.AddComponent<DelegationBridgeHost>();
                var document = view.AddComponent<UIDocument>();
                document.panelSettings = settings;
                var panel = view.AddComponent<C2TopBarPanelHost>();
                panel.enabled = false;
                var root = new VisualElement { name = "c2-topbar-root" };
                foreach (var name in new[]
                {
                    "sim-time-label", "phase-label", "compression-label", "mode-label", "score-label",
                    "compression-slower-button", "compression-faster-button", "pause-resume-button",
                })
                {
                    root.Add(name.EndsWith("button")
                        ? new Button { name = name }
                        : new Label { name = name });
                }

                document.rootVisualElement.Add(root);
                typeof(C2TopBarPanelHost).GetField("bridgeHost", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(panel, host);

                panel.enabled = true;
                yield return null;

                composition.SetActive(true);
                yield return null;
                Assert.That(host.TrySetTimeAcceleration(4, out var reason), Is.True, reason);
                yield return null;

                Assert.That(root.Q<Label>("compression-label").text, Is.EqualTo("TIME: 4x"));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                Object.Destroy(view);
                Object.Destroy(composition);
                Object.Destroy(settings);
                Object.Destroy(theme);
            }
        }

        [UnityTest]
        public IEnumerator Contact_panel_binds_once_per_frame_and_clears_on_unit_selection()
        {
            var composition = new GameObject("slice-a-composition-test");
            var view = new GameObject("slice-a-contact-test");
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            try
            {
                var host = composition.AddComponent<DelegationBridgeHost>();
                host.Bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
                host.Bridge.Orchestrator.DecisionLog.AppendContactChange(
                    new ContactChangeRecord(0, 1, 1, "u1", "c1", "hostile-1", "Unknown", "Identified"));
                host.BeginExecution();
                var world = new World();
                host.RunTick(world, world);
                var provenance = host.LastSliceAContacts.Provenance.Contacts.Single(row => row.ContactId == "c1");
                Assert.That(provenance.Source.ObserverId, Is.EqualTo("u1"));
                Assert.That(provenance.Source.SourceRef, Is.EqualTo("observer:u1|target:hostile-1"));
                Assert.That(provenance.LastKnown.TargetId, Is.EqualTo("hostile-1"));
                Assert.That(provenance.OutOfCommsUnknown, Is.False);
                Assert.That(host.LastSliceAContacts.Authorities, Is.Empty,
                    "The snapshot supplies no shooter-specific authority evidence.");
                host.SelectContact("c1");

                view.SetActive(false);
                var document = view.AddComponent<UIDocument>();
                document.panelSettings = settings;
                var panel = view.AddComponent<ContactDetailPanelHost>();
                typeof(ContactDetailPanelHost).GetField("bridgeHost", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(panel, host);
                view.SetActive(true);
                var root = new VisualElement { name = "contact-detail-root" };
                foreach (var name in new[] { "contact-id-line", "target-id-line", "classification-line",
                    "source-line", "confidence-line", "age-line", "last-known-line", "comms-line",
                    "kill-chain-line", "sensor-shooter-line", "authority-line", "next-action-line" })
                    root.Add(new Label { name = name });
                document.rootVisualElement.Add(root);
                yield return null;

                Assert.That(root.Q<Label>("contact-id-line").text, Does.Contain("c1"));
                Assert.That(root.Q<Label>("authority-line").text, Does.Contain("UNKNOWN"));
                Assert.That(panel.LastLiveSurface.SourceLine, Is.EqualTo("SRC: u1 | ref observer:u1|target:hostile-1"));
                Assert.That(panel.LastLiveSurface.CommsLine, Is.EqualTo("COMMS: no degradation reported"));
                Assert.That(root.Q<Label>("source-line").text, Is.EqualTo(panel.LastLiveSurface.SourceLine));
                Assert.That(root.Q<Label>("comms-line").text, Is.EqualTo(panel.LastLiveSurface.CommsLine));
                Assert.That(root.Q<Label>("source-line").ClassListContains(ContactProvenanceCueClasses.Nominal), Is.True);
                Assert.That(root.Q<Label>("comms-line").ClassListContains(ContactProvenanceCueClasses.Nominal), Is.True);
                var presentation = panel.LastSliceAPresentation;
                var liveSurface = panel.LastLiveSurface;
                yield return null;
                Assert.That(panel.LastSliceAPresentation, Is.SameAs(presentation), "No tick or selection change: reuse presentation.");
                Assert.That(panel.LastLiveSurface, Is.SameAs(liveSurface), "No tick or selection change: reuse provenance.");

                host.SelectUnit("u1");
                yield return null;
                Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.None));
                Assert.That(root.Q<Label>("contact-id-line").text, Does.Not.Contain("c1"));
                Assert.That(panel.LastSliceAPresentation, Is.SameAs(SliceAContactPresentation.Empty));
                Assert.That(panel.LastLiveSurface, Is.SameAs(SliceAContactLiveSurfaceState.Empty));
                Assert.That(root.Q<Label>("source-line").text, Is.EqualTo(SliceAContactLiveSurfaceState.Empty.SourceLine));
                Assert.That(root.Q<Label>("comms-line").text, Is.EqualTo(SliceAContactLiveSurfaceState.Empty.CommsLine));
            }
            finally
            {
                Object.Destroy(view);
                Object.Destroy(composition);
                Object.Destroy(settings);
            }
        }

        private sealed class World : ISimWorldSnapshot, IOrderSink
        {
            public double SimTime => 2;
            public int ContactCount => 1;
            public int ActiveEngagementCount => 0;
            public TargetId? PrimaryHostileContactId => new TargetId("hostile-1");
            public bool HasFireControlTrackOnPrimaryContact => true;
            public bool ObserverRadarEmconActive => true;
            public bool IsMemberAlive(TargetId memberId) => true;
            public void ApplyOrder(EntityKey entity, in Order order) { }
        }
    }
}
#endif
