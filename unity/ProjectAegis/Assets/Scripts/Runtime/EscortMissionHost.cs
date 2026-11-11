#if UNITY_5_3_OR_NEWER
using ProjectAegis.Delegation.Gameplay;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Gameplay;
using UnityEngine;

namespace ProjectAegis.Unity.Runtime
{
    /// <summary>
    /// Scene composition root for the engine-free escort mission. Owns scheduling only;
    /// mission rules, combat, command validation and scoring remain in the headless session.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DelegationBridgeHost))]
    public sealed class EscortMissionHost : MonoBehaviour
    {
        private const double FixedDeltaSeconds = 1.0 / 60.0;
        [SerializeField] private int seed = 42;
        private readonly FixedStepFrameRunner _runner = new(FixedDeltaSeconds);
        private DelegationBridgeHost _presentationHost = null!;
        private float _nextRefresh;

        /// <summary>Engine-free mission, also available to deterministic Play Mode tests.</summary>
        public EscortMissionSession Mission { get; private set; } = null!;

        /// <summary>Read-only mission presentation for UI Toolkit binding.</summary>
        public EscortMissionPresentation Presentation => Mission.Presentation;

        /// <summary>Latest player command acknowledgement or rejection.</summary>
        public string LastCommandMessage { get; private set; } = string.Empty;

        /// <summary>Published C2 presentation; refreshing it never advances the simulation.</summary>
        public DelegationBridgeHost PresentationHost => _presentationHost;

        private void Awake()
        {
            _presentationHost = GetComponent<DelegationBridgeHost>();
            Mission = new EscortMissionSession(seed);
        }

        private void Start() => BindMission();

        private void Update()
        {
            AdvanceFrame(Time.unscaledDeltaTime);
            if (Time.unscaledTime >= _nextRefresh)
            {
                RefreshPresentation();
                _nextRefresh = Time.unscaledTime + 0.2f;
            }
        }

        /// <summary>Convert elapsed render time to fixed steps, checking pause between each step.</summary>
        public void AdvanceFrame(double elapsedSeconds)
        {
            if (Mission == null)
            {
                return;
            }

            _runner.Advance(elapsedSeconds, Mission.Bridge.Session?.TimeAccelerationFactor ?? 1,
                CanStep, StepMission);
        }

        private bool CanStep() => !Mission.IsComplete
            && Mission.Bridge.Phase == SimulationPhase.Executing
            && Mission.Bridge.Session is { IsSimPaused: false };

        private void StepMission() => Mission.Step(FixedDeltaSeconds);

        /// <summary>Forward player intent to the headless command service and display its result.</summary>
        public EscortCommandResult IssueCommand(string commandId)
        {
            var previousBridge = Mission.Bridge;
            var result = Mission.Command(commandId);
            LastCommandMessage = result.Message;
            if (result.Accepted && commandId == "pause")
            {
                _runner.Reset();
            }
            if (!ReferenceEquals(previousBridge, Mission.Bridge))
            {
                _runner.Reset();
                BindMission();
            }
            else
            {
                RefreshPresentation();
            }

            return result;
        }

        private void BindMission()
        {
            _presentationHost.BindExternalSession(Mission.Bridge, seed);
            RefreshPresentation();
        }

        /// <summary>Publish immutable projections; never tick or append demonstration events.</summary>
        public void RefreshPresentation()
        {
            if (Mission == null || _presentationHost == null)
            {
                return;
            }

            var view = Mission.Presentation;
            var messages = new MessageLogLine[view.Moments.Count];
            for (var i = 0; i < messages.Length; i++)
            {
                var moment = view.Moments[i];
                messages[i] = new MessageLogLine((ulong)i, moment.SimTime, "MISSION", moment.Title + ": " + moment.Detail);
            }

            _presentationHost.RefreshExternalPresentation(
                Mission.Snapshot, view.Score, view.RemainingSeconds, view.Status, Mission.MapSymbols, messages);
        }
    }
}
#endif
