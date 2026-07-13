// Play-mode smoke harness: stub snapshot + sink for DelegationBridge (no ECS required).
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using UnityEngine;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    public sealed class SimplePlayModeSimHost : MonoBehaviour, ISimWorldSnapshot, IOrderSink
    {
        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private int contactCount = 2;
        [SerializeField] private bool hasFireControlTrack = true;
        [SerializeField] private string primaryHostileContactId = "hostile-1";
        [SerializeField] private float simTimeStep = 1f / 60f;
        [Tooltip("Smoke harness: auto-start execution. Disable for RTwP planning UX.")]
        [SerializeField] private bool autoBeginOnStart = true;

        private double _simTime;
        private readonly List<(EntityKey Entity, Order Order)> _applied = new();

        // S36-05: frame budget capture support (Unity specialist). Accumulates deltaTime for mean/p95 measurement in PlayMode / Editor host.
        private readonly List<double> _frameTimes = new();
        private double _lastFrameLogTime;

        public double SimTime => _simTime;
        public int ContactCount => contactCount;
        public int ActiveEngagementCount => 0;

        public TargetId? PrimaryHostileContactId =>
            contactCount > 0 ? new TargetId(primaryHostileContactId) : null;

        public bool HasFireControlTrackOnPrimaryContact =>
            contactCount > 0 && hasFireControlTrack;

        public bool ObserverRadarEmconActive => true;

        public IReadOnlyList<(EntityKey Entity, Order Order)> AppliedOrders => _applied;

        private void Reset()
        {
            if (bridgeHost == null)
            {
                bridgeHost = GetComponent<DelegationBridgeHost>();
            }
        }

        private void Start()
        {
            if (autoBeginOnStart && bridgeHost?.Bridge != null)
            {
                bridgeHost.BeginExecution();
            }
        }

        private void Update()
        {
            if (bridgeHost?.Bridge == null)
            {
                return;
            }

            // S36-05 Unity C2 frame budget capture: record Time.deltaTime (includes UI Toolkit + render on Editor host)
            // Use this in PlayMode tests or Editor runner to compute mean/p95 vs 16.67 ms budget. Headless dotnet falls back to projection timing.
            if (Time.frameCount > 1)
            {
                _frameTimes.Add(Time.deltaTime * 1000.0); // ms
            }

            _applied.Clear();
            _simTime += simTimeStep;
            bridgeHost.RunTick(this, this);
        }

        /// <summary>S36-05: expose captured frame times for measurement doc / test (mean/p95 in ms). Call after N frames.</summary>
        public IReadOnlyList<double> CapturedFrameTimesMs => _frameTimes;

        public bool IsMemberAlive(TargetId memberId) => true;

        public void ApplyOrder(EntityKey entity, in Order order) =>
            _applied.Add((entity, order));
    }
}
#endif
