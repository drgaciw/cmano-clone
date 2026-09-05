#if UNITY_5_3_OR_NEWER
using System.Collections;
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
    /// <summary>Last-mile bind/cache/selection checks; does not certify a combat scenario.</summary>
    public sealed class SliceAContactPanelSmokeTests
    {
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
                    "kill-chain-line", "sensor-shooter-line", "authority-line", "next-action-line" })
                    root.Add(new Label { name = name });
                document.rootVisualElement.Add(root);
                yield return null;

                Assert.That(root.Q<Label>("contact-id-line").text, Does.Contain("c1"));
                Assert.That(root.Q<Label>("authority-line").text, Does.Contain("UNKNOWN"));
                var presentation = panel.LastSliceAPresentation;
                yield return null;
                Assert.That(panel.LastSliceAPresentation, Is.SameAs(presentation), "No tick or selection change: reuse presentation.");

                host.SelectUnit("u1");
                yield return null;
                Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.None));
                Assert.That(root.Q<Label>("contact-id-line").text, Does.Not.Contain("c1"));
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
