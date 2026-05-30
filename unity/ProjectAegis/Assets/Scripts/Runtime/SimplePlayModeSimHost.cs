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
        [SerializeField] private float simTimeStep = 1f / 60f;
        [Tooltip("Smoke harness: auto-start execution. Disable for RTwP planning UX.")]
        [SerializeField] private bool autoBeginOnStart = true;

        private double _simTime;
        private readonly List<(EntityKey Entity, Order Order)> _applied = new();

        public double SimTime => _simTime;
        public int ContactCount => contactCount;
        public int ActiveEngagementCount => 0;

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

            _applied.Clear();
            _simTime += simTimeStep;
            bridgeHost.RunTick(this, this);
        }

        public bool IsMemberAlive(TargetId memberId) => true;

        public void ApplyOrder(EntityKey entity, in Order order) =>
            _applied.Add((entity, order));
    }
}
#endif
