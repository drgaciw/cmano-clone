#if UNITY_5_3_OR_NEWER
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Unity.Runtime;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Tests
{
    public sealed class CombatMapViewTests
    {
        [Test]
        public void Timeline_review_clears_live_transients_and_return_to_live_restores_them()
        {
            var bridgeObject = new GameObject("vfx-review-bridge-test");
            var mapObject = new GameObject("vfx-review-map-test");
            bridgeObject.SetActive(false);
            mapObject.SetActive(false);
            try
            {
                var bridge = bridgeObject.AddComponent<DelegationBridgeHost>();
                var host = mapObject.AddComponent<MapPlaceholderPanelHost>();
                var canvas = new VisualElement();
                var effects = new MapCanvasTransientEffectsRenderer(canvas);
                const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(MapPlaceholderPanelHost).GetField("bridgeHost", fields)!.SetValue(host, bridge);
                typeof(MapPlaceholderPanelHost).GetField("_transientEffects", fields)!.SetValue(host, effects);
                var apply = typeof(MapPlaceholderPanelHost).GetMethod("ApplyTransientCombatVfx", fields)!;
                var terminal = new CombatEvent(CombatEventPhase.TerminalOutcome, "s", "t", "Gun", "Kill", 1, 2, 2, "terminal");
                var frame = CombatPresentationFrame.Empty with
                {
                    Events = new CombatEventSnapshot(new[] { terminal }),
                    SimTime = 2,
                };
                var live = CombatVfxProjection.Project(frame.Events,
                    new Dictionary<string, (float X, float Y)> { ["s"] = (.2f, .3f), ["t"] = (.7f, .8f) }, 2);
                typeof(DelegationBridgeHost).GetProperty("LastCombatVfx")!.SetValue(bridge, live);
                typeof(DelegationBridgeHost).GetProperty("LastCombatFrame")!.SetValue(bridge, frame);
                bridge.CommandTimeline.Capture(frame, System.Array.Empty<MapSymbolEntry>());

                apply.Invoke(host, null);
                Assert.That(host.LastCombatVfxFireLineCount, Is.EqualTo(1));
                Assert.That(host.LastCombatVfxImpactCount, Is.EqualTo(1));

                Assert.That(bridge.CommandTimeline.Inspect(bridge.CommandTimeline.Filter(new())[0]), Is.True);
                apply.Invoke(host, null);
                Assert.That(effects.FireLineCount, Is.Zero);
                Assert.That(effects.ImpactMarkerCount, Is.Zero);
                Assert.That(host.LastCombatVfxFireLineCount, Is.Zero);
                Assert.That(host.LastCombatVfxImpactCount, Is.Zero);
                Assert.That(bridge.LastCombatVfx, Is.SameAs(live));

                bridge.CommandTimeline.ReturnToLive();
                apply.Invoke(host, null);
                Assert.That(host.LastCombatVfxFireLineCount, Is.EqualTo(1));
                Assert.That(host.LastCombatVfxImpactCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mapObject);
                UnityEngine.Object.DestroyImmediate(bridgeObject);
            }
        }

        [Test]
        public void History_reuses_bounded_buttons_across_frames_and_detaches_on_dispose()
        {
            var panel = new VisualElement();
            var canvas = new VisualElement();
            panel.Add(canvas);
            var view = new CombatMapView(canvas, panel, _ => { });
            var events = Enumerable.Range(1, 300).Select(i => new CombatEvent(
                CombatEventPhase.AuthorizationRefused, "s", "t", "Gun", "NO_AMMO",
                (ulong)i, i, (ulong)i, "abort:NO_AMMO")).ToArray();
            var frame = CombatPresentationFrame.Empty with { Events = new CombatEventSnapshot(events), SimTime = 300 };
            view.Bind(frame, System.Array.Empty<MapSymbolEntry>(), null);
            var history = panel.Q<ScrollView>("combat-event-history");
            var buttons = history.Query<Button>().ToList();
            Assert.That(buttons.Count, Is.EqualTo(200));
            view.Bind(frame with { SimTime = 301 }, System.Array.Empty<MapSymbolEntry>(), null);
            Assert.That(history.Query<Button>().ToList(), Is.EqualTo(buttons));
            view.SetZoom(CombatZoomBand.Theater);
            Assert.That(history.Query<Button>().ToList(), Is.EqualTo(buttons));
            view.Dispose();
            Assert.That(panel.Q("combat-inspection-controls"), Is.Null);
            Assert.That(canvas.Q("combat-event-layer"), Is.Null);
        }
    }
}
#endif
