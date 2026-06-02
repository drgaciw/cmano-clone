// Optional Unity host — requires Plugins DLLs (see unity/ProjectAegis/README.md).
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using UnityEngine;

namespace ProjectAegis.Unity.Runtime
{
    /// <summary>
    /// Holds a delegation session for play-mode wiring. Assign snapshot/sink from your sim systems.
    /// </summary>
    public sealed class DelegationBridgeHost : MonoBehaviour
    {
        [SerializeField] private int globalSeed = 42;
        [SerializeField] private bool enableMvpEngagement = true;

        public DelegationBridge Bridge { get; private set; } = null!;

        /// <summary>MVP engage session (same orchestrator as <see cref="Bridge"/>).</summary>
        public SimulationSession? Session => Bridge.Session;

        public SimulationPhase Phase =>
            Bridge != null ? Bridge.Phase : SimulationPhase.Planning;

        /// <summary>Combat/AAR message lines projected from the order log (HUD message log).</summary>
        public IReadOnlyList<MessageLogLine> LastMessageLog { get; private set; } = Array.Empty<MessageLogLine>();

        /// <summary>Sensor C2 contact list + EMCON / track indicators for HUD binding.</summary>
        public SensorC2Snapshot LastSensorC2 { get; private set; } =
            new(Array.Empty<ContactPictureEntry>(), 0, true, false, null, 0);

        private void Awake()
        {
            Bridge = new DelegationBridge(globalSeed, mvpEngagement: enableMvpEngagement);
        }

        public void BeginExecution() => Bridge.BeginExecution();

        /// <summary>
        /// Call once per sim step after building snapshot and sink for this frame.
        /// </summary>
        public DelegationTickResult RunTick(ISimWorldSnapshot snapshot, IOrderSink sink)
        {
            var result = Bridge.Tick(snapshot, sink);
            LastMessageLog = MessageLogBridge.ProjectCombatMessages(Bridge.Orchestrator.DecisionLog);
            LastSensorC2 = SensorC2Bridge.Build(snapshot, Bridge.Orchestrator.DecisionLog);
            return result;
        }
    }
}
#endif