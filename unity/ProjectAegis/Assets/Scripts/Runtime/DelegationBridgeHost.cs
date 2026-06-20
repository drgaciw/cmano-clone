// Optional Unity host — requires Plugins DLLs (see unity/ProjectAegis/README.md).
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using UnityEngine;

namespace ProjectAegis.Unity.Runtime
{
    /// <summary>
    /// Holds a delegation session for play-mode wiring. Assign snapshot/sink from your sim systems.
    /// </summary>
    public sealed class DelegationBridgeHost : MonoBehaviour, IC2PresentationFeed
    {
        [SerializeField] private int globalSeed = 42;
        [SerializeField] private bool enableMvpEngagement = true;
        [SerializeField] private string scenarioPolicyId = "baltic-patrol";
        [Tooltip("When true, prefer globe map host (Cesium Phase B). Placeholder map remains default.")]
        [SerializeField] private bool useGlobeMap;
        // useGlobeMap wiring comment (S20 Cesium foundation):
        // - Per CESIUM-SPIKE-SETUP.md: in a dedicated CesiumSpike.unity (do not modify DelegationSmoke), add/duplicate DelegationBridgeHost and toggle useGlobeMap=true via Inspector.
        // - Consumers (future scene builders / conditional map hosts) read UseGlobeMap to activate CesiumGlobe* vs MapPlaceholderPanelHost.
        // - Default=false keeps Phase A Toolkit map as vertical slice default. Flag is presentation-only; zero impact on RunTick, selection (C2PresentationController), or sim determinism.
        // - GitNexus impact: LOW (0 upstream). See DelegationBridgeHost.UseGlobeMap + Cesium* (presentation layer only).
        // - S24-08: DelegationSmoke.unity keeps useGlobeMap=false (CI-safe); CesiumSpike.unity is the Editor polish scene per CESIUM-SPIKE-SETUP.md.
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

        // S37-04: C2 graph surfacing (dependency viewer/panel/highlights/bind) — read-only catalog projections only
        public IReadOnlyList<string> LastGraphHighlightIds { get; private set; } = Array.Empty<string>();
        public string? LastGraphLinkChainDisplay { get; private set; }

        // S37-04/05: catalog for graph/FK surfacing (injected read-only; no bridge writes)
        public ProjectAegis.Data.Catalog.ICatalogReader? CatalogReader { get; set; }

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

        // S37-04: bind graph surfacing after selection (viewer/panel/highlights + bind)
        private void ApplyGraphSurfacingForSelection()
        {
            if (Presentation != null && CatalogReader != null)
            {
                Presentation.ApplyGraphSurfacing(CatalogReader);
                LastGraphHighlightIds = Presentation.LastGraphHighlightIds;
                LastGraphLinkChainDisplay = Presentation.LastGraphLinkChainDisplay;
            }
            else
            {
                LastGraphHighlightIds = Array.Empty<string>();
                LastGraphLinkChainDisplay = "(graph n/a)";
            }
        }

        /// <summary>Interactive attack menu selection (req 14).</summary>
        public bool TrySelectAttackOption(string optionId, out string? failureReason)
        {
            failureReason = null;
            if (_lastSnapshot == null || string.IsNullOrEmpty(SelectedUnitId))
            {
                failureReason = "NO_SELECTION";
                return false;
            }

            if (!TryResolveEntityKey(SelectedUnitId, out var entityKey))
            {
                failureReason = "UNKNOWN_UNIT";
                return false;
            }

            EnsureHumanControl(entityKey);
            return Bridge.TryEnqueueAttackOption(entityKey, optionId, _lastSnapshot, out failureReason);
        }

        public void SelectUnit(string unitId)
        {
            Presentation.SelectFriendlyUnit(unitId);
            RefreshSelectionPresentation();
            ApplyGraphSurfacingForSelection();  // S37-04 C2 graph surfacing (highlights/bind)
        }

        public void SelectContact(string contactId)
        {
            var contacts = ContactPictureProjection.Project(Bridge.Orchestrator.DecisionLog);
            Presentation.SelectHostileContact(contactId, contacts);
            RefreshSelectionPresentation();
            // contacts do not drive platform graph
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
            ApplyGraphSurfacingForSelection(); // S37-04 ensure graph after default select
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

        private bool TryResolveEntityKey(string unitId, out EntityKey entityKey)
        {
            entityKey = default;
            foreach (var binding in Bridge.Registry.Bindings)
            {
                if (string.Equals(binding.TargetId.Value, unitId, StringComparison.Ordinal))
                {
                    entityKey = binding.Entity;
                    return true;
                }
            }

            return false;
        }

        private void EnsureHumanControl(EntityKey entityKey)
        {
            if (!Bridge.Registry.TryGetBinding(entityKey, out var binding))
            {
                return;
            }

            if (binding.Target.Slot.Active is ProjectAegis.Delegation.Controllers.HumanController)
            {
                return;
            }

            binding.Target.Slot.SetActive(new ProjectAegis.Delegation.Controllers.HumanController());
        }

        public DoctrineInheritanceEntry? LastDoctrineInheritance { get; private set; }

        public bool TrySetDoctrineOverride(string roeLevelLabel)
        {
            if (Bridge == null || string.IsNullOrEmpty(SelectedUnitId))
            {
                return false;
            }

            var unitId = new TargetId(SelectedUnitId);
            var simTime = Bridge.Phase == SimulationPhase.Executing ? _lastSnapshot?.SimTime ?? 0 : 0;

            var result = DoctrineOverrideCommand.TryApply(Bridge.Orchestrator, unitId, roeLevelLabel, simTime);

            if (result)
            {
                RefreshDoctrineInheritance();
            }

            return result;
        }

        public void RefreshDoctrineInheritance()
        {
            if (Bridge == null || string.IsNullOrEmpty(SelectedUnitId))
            {
                LastDoctrineInheritance = null;
                return;
            }

            var unitId = new TargetId(SelectedUnitId);
            var policy = Bridge.Orchestrator.ScenarioPolicy;
            LastDoctrineInheritance = DoctrineInheritanceProjection.ProjectUnit(unitId, policy, isFriendly: true);
        }
    }
}
#endif