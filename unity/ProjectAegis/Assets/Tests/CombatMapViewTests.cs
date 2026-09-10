#if UNITY_5_3_OR_NEWER
using System.Linq;
using NUnit.Framework;
using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Unity.Runtime;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Tests
{
    public sealed class CombatMapViewTests
    {
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
