#if UNITY_5_3_OR_NEWER
using System.Collections;
using NUnit.Framework;
using ProjectAegis.Unity.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProjectAegis.Unity.Tests
{
    /// <summary>Exercises the actual mission host lifecycle and player command boundary.</summary>
    public sealed class EscortMissionPlayModeTests
    {
        [UnityTest]
        public IEnumerator Mission_starts_in_planning_and_refreshing_panels_does_not_advance_time()
        {
            var root = new GameObject("escort-planning-test");
            try
            {
                var host = root.AddComponent<EscortMissionHost>();
                yield return null;
                Assert.That(host.Mission, Is.Not.Null);
                Assert.That(host.Mission.SimTime, Is.EqualTo(0));
                host.AdvanceFrame(1);
                Assert.That(host.Mission.SimTime, Is.EqualTo(0));
                Assert.That(root.GetComponent<DelegationBridgeHost>().Bridge, Is.SameAs(host.Mission.Bridge));
                Assert.That(root.GetComponent<DelegationBridgeHost>().LastMapSymbols, Is.Not.Empty);
                Assert.That(root.GetComponent<DelegationBridgeHost>().LastOobTree.Count, Is.EqualTo(2),
                    "A hidden opposing unit must not appear as a friendly OOB row.");
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator Pause_and_restart_use_fresh_authoritative_state()
        {
            var root = new GameObject("escort-restart-test");
            try
            {
                var host = root.AddComponent<EscortMissionHost>();
                yield return null;
                Assert.That(host.IssueCommand("alerts-manual").Accepted, Is.True);
                Assert.That(host.IssueCommand("begin").Accepted, Is.True);
                host.AdvanceFrame(0.1);
                Assert.That(host.Mission.SimTime, Is.GreaterThan(0));
                Assert.That(host.IssueCommand("pause").Accepted, Is.True);
                var pausedTime = host.Mission.SimTime;
                host.AdvanceFrame(10);
                Assert.That(host.Mission.SimTime, Is.EqualTo(pausedTime));
                var oldBridge = host.Mission.Bridge;
                Assert.That(host.IssueCommand("restart").Accepted, Is.True);
                Assert.That(host.Mission.SimTime, Is.EqualTo(0));
                Assert.That(host.Mission.Bridge, Is.Not.SameAs(oldBridge));
                Assert.That(root.GetComponent<DelegationBridgeHost>().Bridge, Is.SameAs(host.Mission.Bridge));
            }
            finally
            {
                Object.Destroy(root);
            }
        }
    }
}
#endif
