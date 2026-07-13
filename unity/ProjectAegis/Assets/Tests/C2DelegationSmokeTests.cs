// PlayMode smoke test for the C2 delegation bridge seam.
// Automates the manual PLAYMODE-SMOKE.md checklist (fix #2, unity-integration-review):
// build a DelegationBridgeHost with a stub world snapshot/sink, tick several Play Mode
// frames, and assert the C2 projection surface populates without throwing — guarding the
// Unity <-> headless seam in CI.
#if UNITY_5_3_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
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
            public TargetId? PrimaryHostileContactId => new TargetId("hostile-1");
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
            }
            finally
            {
                Object.Destroy(go);
            }
        }
    }
}
#endif
