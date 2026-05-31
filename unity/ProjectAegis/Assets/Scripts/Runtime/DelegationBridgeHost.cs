// Optional Unity host — requires Plugins DLLs (see unity/ProjectAegis/README.md).
#if UNITY_5_3_OR_NEWER
using ProjectAegis.Delegation.Orchestration;
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

        public SimulationPhase Phase =>
            Bridge != null ? Bridge.Phase : SimulationPhase.Planning;

        private void Awake()
        {
            Bridge = new DelegationBridge(globalSeed);
            if (enableMvpEngagement)
            {
                Bridge.EnableMvpEngagement();
            }
        }

        public void BeginExecution() => Bridge.BeginExecution();

        /// <summary>
        /// Call once per sim step after building snapshot and sink for this frame.
        /// </summary>
        public DelegationTickResult RunTick(ISimWorldSnapshot snapshot, IOrderSink sink) =>
            Bridge.Tick(snapshot, sink);
    }
}
#endif
