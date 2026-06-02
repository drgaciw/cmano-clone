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
        [SerializeField] private string scenarioPolicyId = "baltic-patrol";
        [Tooltip("When true, prefer globe map host (Cesium Phase B). Placeholder map remains default.")]
        [SerializeField] private bool useGlobeMap;
        [SerializeField] private string timeCompressionLabel = "1x";
        [SerializeField] private string simulationModeLabel = "Mixed";

        public string ScenarioPolicyId => scenarioPolicyId;

        public bool UseGlobeMap => useGlobeMap;

        public C2PresentationController Presentation { get; } = new();

        public string? SelectedUnitId => Presentation.SelectedUnitId;

        public string? SelectedContactId => Presentation.SelectedContactId;

        public DelegationBridge Bridge { get; private set; } = null!;

        /// <summary>MVP engage session (same orchestrator as <see cref="Bridge"/>).</summary>
        public SimulationSession? Session => Bridge.Session;

        public SimulationPhase Phase =>
            Bridge != null ? Bridge.Phase : SimulationPhase.Planning;

        /// <summary>Full AAR message log (all projected categories) for doc-20 bottom strip.</summary>
        public IReadOnlyList<MessageLogLine> LastMessageLog { get; private set; } = Array.Empty<MessageLogLine>();

        /// <summary>OOB unit rows for doc-20 left drawer.</summary>
        public IReadOnlyList<OobTreeEntry> LastOobTree { get; private set; } = Array.Empty<OobTreeEntry>();

        /// <summary>Mission timeline rows from scenario policy (static per session).</summary>
        public IReadOnlyList<MissionListEntry> LastMissionList { get; private set; } = Array.Empty<MissionListEntry>();

        /// <summary>Primary friendly unit detail for doc-20 right panel.</summary>
        public UnitDetailEntry? LastUnitDetail { get; private set; }

        /// <summary>Tactical map symbols (placeholder layout).</summary>
        public IReadOnlyList<MapSymbolEntry> LastMapSymbols { get; private set; } = Array.Empty<MapSymbolEntry>();

        /// <summary>Top bar labels (time, phase, score).</summary>
        public C2TopBarState LastTopBar { get; private set; } =
            new("SIM 00:00:00", "PHASE: Planning", "TIME: 1x", "MODE: —", "COMMS: NOMINAL", "SCORE: 0");

        /// <summary>Sensor C2 contact list + EMCON / track indicators for HUD binding.</summary>
        public SensorC2Snapshot LastSensorC2 { get; private set; } =
            new(Array.Empty<ContactPictureEntry>(), 0, true, false, null, 0);

        private ISimWorldSnapshot? _lastSnapshot;

        private void Awake()
        {
            Bridge = new DelegationBridge(
                globalSeed,
                mvpEngagement: enableMvpEngagement,
                scenarioPolicyId: scenarioPolicyId);
            LastMissionList = MissionListBridge.ProjectFrom(Bridge.Orchestrator.ScenarioPolicy?.MissionTimeline);
        }

        public void BeginExecution() => Bridge.BeginExecution();

        public void SelectUnit(string unitId)
        {
            Presentation.SelectFriendlyUnit(unitId);
            RefreshSelectionPresentation();
        }

        public void SelectContact(string contactId)
        {
            var contacts = ContactPictureProjection.Project(Bridge.Orchestrator.DecisionLog);
            Presentation.SelectHostileContact(contactId, contacts);
            RefreshSelectionPresentation();
        }

        /// <summary>
        /// Call once per sim step after building snapshot and sink for this frame.
        /// </summary>
        public DelegationTickResult RunTick(ISimWorldSnapshot snapshot, IOrderSink sink)
        {
            _lastSnapshot = snapshot;
            var result = Bridge.Tick(snapshot, sink);
            LastMessageLog = MessageLogBridge.ProjectFrom(Bridge.Orchestrator.DecisionLog);
            LastOobTree = OobTreeBridge.Build(snapshot, Bridge.Registry);
            Presentation.ApplyDefaultSelection(LastOobTree);
            LastSensorC2 = SensorC2Bridge.Build(snapshot, Bridge.Orchestrator.DecisionLog);
            LastUnitDetail = Presentation.ResolveUnitDetail(snapshot, Bridge.Registry, Bridge);
            LastMapSymbols = MapPictureBridge.Build(
                snapshot,
                Bridge.Registry,
                Bridge.Orchestrator.DecisionLog,
                globalSeed);
            LastTopBar = C2TopBarProjection.Project(
                snapshot.SimTime,
                Bridge.Phase,
                timeCompressionLabel,
                simulationModeLabel,
                Bridge.Orchestrator.DecisionLog);
            return result;
        }

        private void RefreshSelectionPresentation()
        {
            if (_lastSnapshot == null)
            {
                return;
            }

            LastUnitDetail = Presentation.ResolveUnitDetail(_lastSnapshot, Bridge.Registry, Bridge);
        }
    }
}
#endif