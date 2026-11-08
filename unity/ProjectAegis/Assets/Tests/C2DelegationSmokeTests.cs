// PlayMode smoke test for the C2 delegation bridge seam.
// Automates the manual PLAYMODE-SMOKE.md checklist (fix #2, unity-integration-review):
// build a DelegationBridgeHost with a stub world snapshot/sink, tick several Play Mode
// frames, and assert the C2 projection surface populates without throwing — guarding the
// Unity <-> headless seam in CI.
#if UNITY_5_3_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Unity.Runtime;

namespace ProjectAegis.Unity.Tests
{
    public sealed class C2DelegationSmokeTests
    {
        /// <summary>Minimal deterministic world matching <see cref="SimplePlayModeSimHost"/>'s stub contract.</summary>
        private sealed class StubWorld : ISimWorldSnapshot, IOrderSink
        {
            public double SimTime { get; set; }
            public int ContactCount => 2;
            public int ActiveEngagementCount => 0;
            public TargetId? PrimaryHostileContactId => new TargetId(SimplePlayModeSimHost.SmokeHostileUnitId);
            public bool HasFireControlTrackOnPrimaryContact => true;
            public bool ObserverRadarEmconActive => true;
            public bool IsMemberAlive(TargetId memberId) => true;

            public readonly List<(EntityKey Entity, Order Order)> Applied = new();
            public void ApplyOrder(EntityKey entity, in Order order) => Applied.Add((entity, order));
        }

        [UnityTest]
        public IEnumerator Bridge_ticks_and_populates_c2_projections()
        {
            // The com.ivanmurzak.unity.mcp dev plugin (and Entities world bootstrap) log SignalR /
            // deserialize exceptions in headless Play Mode with no MCP server attached. Those are
            // unrelated to the seam under test, so don't let the runner's global log handler fail us
            // on them — our explicit Assert.* below still enforce the real behavior.
            LogAssert.ignoreFailingMessages = true;

            var go = new GameObject("c2-smoke");
            try
            {
                var host = go.AddComponent<DelegationBridgeHost>();
                yield return null; // allow Awake() to construct the Bridge

                Assert.IsNotNull(host.Bridge, "DelegationBridgeHost.Awake should construct the Bridge.");

                // Same seed path as SimplePlayModeSimHost (ORBAT + DecisionLog before BeginExecution).
                Assert.IsTrue(
                    SimplePlayModeSimHost.TrySeedSmokeOrbat(host.Bridge),
                    "Smoke ORBAT seed should register u1 / hostile-1.");
                host.BeginExecution();

                var world = new StubWorld();
                for (int i = 0; i < 5; i++)
                {
                    world.SimTime += 1.0 / 60.0;
                    var captured = world;
                    var subject = host;
                    Assert.DoesNotThrow(() => subject.RunTick(captured, captured),
                        "RunTick should not throw during Play Mode ticks.");
                    yield return null;
                }

                Assert.AreEqual(SimulationPhase.Executing, host.Phase,
                    "Bridge should be in the Executing phase after BeginExecution + ticks.");
                Assert.IsNotNull(host.LastTopBar, "Top bar projection should be populated after ticking.");
                Assert.IsNotNull(host.LastMessageLog, "Message log projection should be populated after ticking.");
                Assert.IsNotNull(host.LastMapSymbols, "Map symbol projection should be populated after ticking.");
                Assert.IsNotNull(host.LastOobTree, "OOB tree projection should be populated after ticking.");

                Assert.IsNotEmpty(host.LastOobTree,
                    "OOB tree must list seeded smoke units (u1 / hostile-1).");
                Assert.IsNotNull(host.LastUnitDetail,
                    "Unit Detail must resolve a primary unit after ORBAT seed + default selection.");
                Assert.AreNotEqual("—", host.LastUnitDetail!.UnitId,
                    "Unit Detail unit id must not be the empty placeholder.");
                Assert.IsTrue(
                    host.LastMessageLog.Any(m => m.Category is "CONTACT" or "MAGAZINE" or "MODE"),
                    "Message log must include smoke contact/magazine and/or mode-change rows.");
            }
            finally
            {
                Object.Destroy(go);
            }
        }

        [UnityTest]
        public IEnumerator Left_drawer_oob_selection_returns_from_contact_to_friendly_unit()
        {
            var composition = new GameObject("c2-selection-composition");
            var view = new GameObject("c2-selection-drawer");
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            var theme = ScriptableObject.CreateInstance<ThemeStyleSheet>();
            settings.themeStyleSheet = theme;
            try
            {
                var bridgeHost = composition.AddComponent<DelegationBridgeHost>();
                yield return null;
                Assert.IsTrue(SimplePlayModeSimHost.TrySeedSmokeOrbat(bridgeHost.Bridge));
                bridgeHost.BeginExecution();

                var world = new StubWorld { SimTime = 1.0 };
                bridgeHost.RunTick(world, world);

                var document = view.AddComponent<UIDocument>();
                document.panelSettings = settings;
                var drawer = view.AddComponent<C2LeftDrawerPanelHost>();
                drawer.enabled = false;
                var root = new VisualElement { name = "c2-drawer-root" };
                root.Add(new VisualElement { name = "c2-drawer-tabs" });
                root.Add(new Toggle { name = "tab-oob" });
                root.Add(new Toggle { name = "tab-missions" });
                root.Add(new Toggle { name = "tab-contacts" });
                root.Add(new ListView { name = "oob-list" });
                root.Add(new ListView { name = "mission-list" });
                root.Add(new ListView { name = "contact-list" });
                document.rootVisualElement.Add(root);

                typeof(C2LeftDrawerPanelHost)
                    .GetField("bridgeHost", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(drawer, bridgeHost);
                drawer.enabled = true;
                yield return null;

                var oobList = root.Q<ListView>("oob-list");
                Assert.That(
                    typeof(C2LeftDrawerPanelHost)
                        .GetField("_oobList", BindingFlags.Instance | BindingFlags.NonPublic)!
                        .GetValue(drawer),
                    Is.SameAs(oobList),
                    "The host must be wired to the attached OOB ListView exercised by this test.");
                var friendlyIndex = bridgeHost.LastOobTree
                    .Select((entry, index) => (entry, index))
                    .Single(pair => pair.entry.UnitId == "u1")
                    .index;
                oobList.SetSelection(friendlyIndex);
                yield return null;

                Assert.That(bridgeHost.SelectedUnitId, Is.EqualTo("u1"));
                var selectionChangedCount = 0;
                oobList.selectionChanged += _ => selectionChangedCount++;
                bridgeHost.SelectContact("hostile-1");
                yield return null;
                Assert.That(bridgeHost.SelectedUnitId, Is.Null);
                Assert.That(oobList.selectedIndex, Is.EqualTo(-1),
                    "Contact selection must clear the OOB ListView so the same friendly row can be chosen again.");
                Assert.That(selectionChangedCount, Is.Zero,
                    "Refreshing projected selection must not raise ListView selection callbacks.");

                oobList.SetSelection(friendlyIndex);
                yield return null;

                Assert.That(selectionChangedCount, Is.EqualTo(1));
                Assert.That(bridgeHost.SelectedUnitId, Is.EqualTo("u1"));
                Assert.That(bridgeHost.SelectedContactId, Is.Null);
                Assert.That(bridgeHost.LastUnitDetail?.UnitId, Is.EqualTo("u1"));
            }
            finally
            {
                Object.Destroy(view);
                Object.Destroy(composition);
                Object.Destroy(settings);
                Object.Destroy(theme);
            }
        }
    }
}
#endif
