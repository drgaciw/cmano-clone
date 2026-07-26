// Play-mode smoke harness: stub snapshot + sink for DelegationBridge (no ECS required).
// Seeds a minimal ORBAT + order-log rows so C2 panels (OOB, Unit Detail, Message Log) bind live data.
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using UnityEngine;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    public sealed class SimplePlayModeSimHost : MonoBehaviour, ISimWorldSnapshot, IOrderSink
    {
        public const string SmokeFriendlyUnitId = PlayModeSmokeOrbatSeeder.FriendlyUnitId;
        public const string SmokeHostileUnitId = PlayModeSmokeOrbatSeeder.HostileUnitId;
        public const string SmokeContactId = PlayModeSmokeOrbatSeeder.ContactId;

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private int contactCount = 2;
        [SerializeField] private bool hasFireControlTrack = true;
        [SerializeField] private string primaryHostileContactId = SmokeHostileUnitId;
        [SerializeField] private float simTimeStep = 1f / 60f;
        [Tooltip("Smoke harness: auto-start execution. Disable for RTwP planning UX.")]
        [SerializeField] private bool autoBeginOnStart = true;
        [Tooltip("Register smoke ORBAT + seed DecisionLog so Unit Detail / OOB / Message Log are non-empty.")]
        [SerializeField] private bool seedSmokeOrbatOnStart = true;

        private double _simTime;
        private readonly List<(EntityKey Entity, Order Order)> _applied = new();
        private bool _orbatSeeded;

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

        /// <summary>True after <see cref="SeedSmokeOrbat"/> registered members on the bridge registry.</summary>
        public bool OrbatSeeded => _orbatSeeded;

        private void Reset()
        {
            if (bridgeHost == null)
            {
                bridgeHost = GetComponent<DelegationBridgeHost>();
            }
        }

        private void Start()
        {
            if (bridgeHost?.Bridge == null)
            {
                return;
            }

            if (seedSmokeOrbatOnStart)
            {
                SeedSmokeOrbat(bridgeHost.Bridge);
                // Prefer friendly primary: OOB sort is alphabetical and would otherwise pick hostile-1 first.
                bridgeHost.SelectUnit(SmokeFriendlyUnitId);
            }

            if (autoBeginOnStart)
            {
                bridgeHost.BeginExecution();
            }
        }

        /// <summary>
        /// Registers friendly/hostile smoke units, configures Mixed mode, and appends contact/magazine
        /// rows so Message Log / OOB / Unit Detail have data without a full Baltic harness load.
        /// Delegates to shipped <see cref="PlayModeSmokeOrbatSeeder"/>.
        /// </summary>
        public void SeedSmokeOrbat(DelegationBridge bridge)
        {
            if (_orbatSeeded)
            {
                return;
            }

            _orbatSeeded = TrySeedSmokeOrbat(bridge);
        }

        /// <summary>
        /// Static seed entry for tests / batch runners that hold a <see cref="DelegationBridge"/> only.
        /// Forwards to <see cref="PlayModeSmokeOrbatSeeder.TrySeed"/>.
        /// </summary>
        public static bool TrySeedSmokeOrbat(DelegationBridge? bridge) =>
            PlayModeSmokeOrbatSeeder.TrySeed(bridge);

        /// <summary>Append contact + magazine rows projected into the Message Log strip.</summary>
        public static void SeedSmokeDecisionLog(DecisionLog log) =>
            PlayModeSmokeOrbatSeeder.SeedDecisionLog(log);

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
