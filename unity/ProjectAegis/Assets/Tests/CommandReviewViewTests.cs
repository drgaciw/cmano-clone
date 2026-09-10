#if UNITY_5_3_OR_NEWER
using System;
using System.Linq;
using NUnit.Framework;
using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Unity.Runtime;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Tests
{
    public sealed class CommandReviewViewTests
    {
        [Test]
        public void Timeline_rows_reuse_buttons_and_document_recreation_binds_while_paused()
        {
            var go = new GameObject("command-review-test");
            go.SetActive(false);
            try
            {
                var host = go.AddComponent<DelegationBridgeHost>();
                var rows = Enumerable.Range(1, 150).Select(i => new CombatEvent(CombatEventPhase.Firing,
                    "s", "t", "Gun", "Launch", (ulong)i, i, (ulong)i, "launch")).ToArray();
                host.CommandTimeline.Capture(CombatPresentationFrame.Empty with
                    { SimTime = 150, Events = new CombatEventSnapshot(rows) }, Array.Empty<MapSymbolEntry>());
                var parent = new VisualElement();
                var view = new CommandReviewView(parent, host);
                view.Bind();
                var first = parent.Q<ScrollView>("command-review-events").Query<Button>().ToList();
                Assert.That(first.Count, Is.EqualTo(100));
                view.Bind();
                Assert.That(parent.Q<ScrollView>("command-review-events").Query<Button>().ToList(), Is.EqualTo(first));
                host.InspectTimelineEvent(host.CommandTimeline.Filter(new())[0]);
                view.Bind();
                Assert.That(parent.Q("command-review-decisions").enabledSelf, Is.False);
                Assert.That(host.SubmitGroupDecision("unknown", ProjectAegis.Delegation.UnityAdapter.CommandReview.CoordinationDecision.Hold),
                    Does.Contain("Return to live"));
                host.ReturnToLiveCombat();
                view.Bind();
                Assert.That(parent.Q("command-review-decisions").enabledSelf, Is.True);
                view.Dispose();
                Assert.That(parent.Q("command-review"), Is.Null);
                using var rebound = new CommandReviewView(parent, host);
                rebound.Bind();
                Assert.That(parent.Q<ScrollView>("command-review-events").Query<Button>().ToList().Count, Is.EqualTo(100));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
#endif
