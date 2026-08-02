// Optional Unity host — requires Plugins DLLs (see unity/ProjectAegis/README.md).
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Input;
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

        /// <summary>
        /// Domain keys with recent combat activity for ASSET-021 hot-tick HUD (headless binder input).
        /// Derived from message-log combat categories — no sim hot-path coupling.
        /// </summary>
        public IReadOnlyList<string> LastCombatDomainActivityTags { get; private set; } = Array.Empty<string>();

        /// <summary>OOB unit rows for doc-20 left drawer.</summary>
        public IReadOnlyList<OobTreeEntry> LastOobTree { get; private set; } = Array.Empty<OobTreeEntry>();

        /// <summary>Mission timeline rows from scenario policy (static per session).</summary>
        public IReadOnlyList<MissionListEntry> LastMissionList { get; private set; } = Array.Empty<MissionListEntry>();

        /// <summary>Primary friendly unit detail for doc-20 right panel.</summary>
        public UnitDetailEntry? LastUnitDetail { get; private set; }

        /// <summary>Tactical map symbols (placeholder layout).</summary>
        public IReadOnlyList<MapSymbolEntry> LastMapSymbols { get; private set; } = Array.Empty<MapSymbolEntry>();

        /// <summary>Top bar labels (time, phase, score, CMD-22 Zulu/local/remain).</summary>
        public C2TopBarState LastTopBar { get; private set; } =
            new("SIM 00:00:00", "PHASE: Planning", "TIME: 1x", "MODE: —", "COMMS: NOMINAL", "SCORE: 0",
                "ZULU 00:00:00", "LOCAL 00:00:00", "REMAIN —");

        /// <summary>Sensor C2 contact list + EMCON / track indicators for HUD binding.</summary>
        public SensorC2Snapshot LastSensorC2 { get; private set; } =
            new(Array.Empty<ContactPictureEntry>(), 0, true, false, null, 0);

        // S37-04: C2 graph surfacing (dependency viewer/panel/highlights/bind) — read-only catalog projections only
        public IReadOnlyList<string> LastGraphHighlightIds { get; private set; } = Array.Empty<string>();
        public string? LastGraphLinkChainDisplay { get; private set; }

        // S37-04/05: catalog for graph/FK surfacing (injected read-only; no bridge writes)
        public ProjectAegis.Data.Catalog.ICatalogReader? CatalogReader { get; set; }

        /// <summary>CMD-37 agent roster rows (presentation projection from registry controllers).</summary>
        public IReadOnlyList<AgentRosterEntry> LastAgentRoster { get; private set; } = Array.Empty<AgentRosterEntry>();

        /// <summary>CMD-24 Phase A air-ops readiness rows (from Session.UnitReadiness + OOB).</summary>
        public IReadOnlyList<AirOpsEntry> LastAirOps { get; private set; } = Array.Empty<AirOpsEntry>();

        /// <summary>True when Session.UnitReadiness is present (false → honest NO READINESS DATA).</summary>
        public bool HasAirOpsReadinessData { get; private set; }

        /// <summary>CMD-24 magazine loadout rows (from Session.Magazines snapshot when present).</summary>
        public IReadOnlyList<MagazineLoadoutEntry> LastMagazineLoadout { get; private set; } = Array.Empty<MagazineLoadoutEntry>();

        /// <summary>True when Session.Magazines is present (false → honest NO MAGAZINE DATA).</summary>
        public bool HasMagazineLoadoutData { get; private set; }

        /// <summary>CMD-24 deck/hangar capacity rows (inject via SetDeckHangarFeed; no session source yet).</summary>
        public IReadOnlyList<DeckHangarCapacityEntry> LastDeckHangar { get; private set; } = Array.Empty<DeckHangarCapacityEntry>();

        /// <summary>True when a deck/hangar capacity feed has been bound (false → honest NO CAPACITY DATA).</summary>
        public bool HasDeckHangarData { get; private set; }

        /// <summary>
        /// CMD-23 UI-local chrome collapse flags (message log + left drawer).
        /// Not DecisionLog; persisted only via in-memory <see cref="C2ChromePrefsStore"/>.
        /// </summary>
        public C2ChromeCollapseState ChromeCollapse { get; private set; } = C2ChromeCollapseState.Expanded;

        private readonly C2ChromePrefsStore _chromePrefs = new();
        private ISimWorldSnapshot? _lastSnapshot;

        /// <summary>Latest live simulation tick for presentation staleness calculations.</summary>
        public ulong CurrentSimTick => (ulong)Math.Max(0, (long)(_lastSnapshot?.SimTime ?? 0));

        private void Awake()
        {
            Bridge = new DelegationBridge(
                globalSeed,
                mvpEngagement: enableMvpEngagement,
                scenarioPolicyId: scenarioPolicyId);
            LastMissionList = MissionListBridge.ProjectFrom(Bridge.Orchestrator.ScenarioPolicy?.MissionTimeline);
            // Restore UI-local chrome collapse bag (in-memory; no DecisionLog / file I/O).
            ChromeCollapse = _chromePrefs.Restore(C2ChromeCollapseState.Expanded);
        }

        /// <summary>
        /// CMD-23: toggle message-log body collapse and capture into the in-memory prefs bag.
        /// </summary>
        public void ToggleMessageLogCollapsed()
        {
            ChromeCollapse = ChromeCollapse.ToggleMessageLog();
            _chromePrefs.Capture(ChromeCollapse);
        }

        /// <summary>
        /// CMD-23: toggle left-drawer body collapse and capture into the in-memory prefs bag.
        /// </summary>
        public void ToggleLeftDrawerCollapsed()
        {
            ChromeCollapse = ChromeCollapse.ToggleLeftDrawer();
            _chromePrefs.Capture(ChromeCollapse);
        }

        /// <summary>
        /// CMD-23: replace chrome collapse state (tests / prefs restore) and capture.
        /// </summary>
        public void SetChromeCollapse(C2ChromeCollapseState state)
        {
            ChromeCollapse = state ?? C2ChromeCollapseState.Expanded;
            _chromePrefs.Capture(ChromeCollapse);
        }

        /// <summary>CMD-23: copy of the current chrome prefs bag (host-free tests / tooling).</summary>
        public IReadOnlyDictionary<string, bool> GetChromePrefsSnapshot() => _chromePrefs.GetSnapshot();

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

        /// <summary>
        /// CMD-31: issue a player command for the currently selected unit (unit-order toolbar).
        /// Does not rewrite RunTick; returns structured failure reasons for disabled UI chrome.
        /// </summary>
        public bool TryIssueSelectedCommand(string commandId, out string? reason)
        {
            reason = null;
            if (string.IsNullOrEmpty(SelectedUnitId))
            {
                reason = ProjectAegis.Delegation.Input.C2CommandIssuance.ReasonNoSelection;
                return false;
            }

            if (!TryResolveEntityKey(SelectedUnitId, out var entityKey))
            {
                reason = ProjectAegis.Delegation.UnityAdapter.Bridge.C2PlayerCommandBridge.ReasonUnknownUnit;
                return false;
            }

            if (!IsPlayerControlledUnit(entityKey))
            {
                reason = "NOT_PLAYER_CONTROLLED";
                return false;
            }

            var simTime = _lastSnapshot?.SimTime ?? 0;
            return C2PlayerCommandBridge.TryIssue(Bridge, entityKey, commandId, simTime, out reason);
        }

        /// <summary>
        /// LOG-08 / CMD-24 Phase N: launch the selected aircraft (order-log path).
        /// Thin wrapper over C2CommandIssuance + human enqueue; no Tick rewrite.
        /// </summary>
        public bool TryLaunchSelectedAircraft(out string? reason) =>
            TryIssueSelectedCommand("launch", out reason);

        /// <summary>
        /// LOG-08 / CMD-24 Phase N: abort launch for the selected aircraft (order-log path).
        /// </summary>
        public bool TryAbortLaunchSelected(out string? reason) =>
            TryIssueSelectedCommand("abort_launch", out reason);

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
            LastCombatDomainActivityTags = CombatDomainActivityTags.FromMessageLog(LastMessageLog);
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
            // CMD-37: additive roster projection (no Tick body rewrite)
            LastAgentRoster = BuildAgentRosterFromRegistry();
            // CMD-24 Phase A: additive air-ops readiness projection
            RefreshAirOps();
            // CMD-24 Wave4: magazine + deck/hangar feeds (no Tick body rewrite)
            RefreshMagazineLoadout();
            RefreshDeckHangar();
            return result;
        }

        /// <summary>
        /// Rebuild agent roster from registry controller slots (safe to call from LateUpdate).
        /// </summary>
        public void RefreshAgentRoster()
        {
            if (Bridge == null)
            {
                LastAgentRoster = Array.Empty<AgentRosterEntry>();
                return;
            }

            LastAgentRoster = BuildAgentRosterFromRegistry();
        }

        /// <summary>
        /// Rebuild CMD-24 Air Ops rows from Session.AirOps lifecycle when present,
        /// else Session.UnitReadiness + OOB (safe from LateUpdate).
        /// Honest empty when neither lifecycle ledger nor readiness map has data.
        /// </summary>
        public void RefreshAirOps()
        {
            if (Bridge?.Session == null)
            {
                HasAirOpsReadinessData = false;
                LastAirOps = Array.Empty<AirOpsEntry>();
                return;
            }

            // LOG-08 Phase N: prefer session air-ops FSM snapshot when units are tracked.
            var airOps = Bridge.Session.AirOps;
            if (airOps != null && airOps.Count > 0)
            {
                HasAirOpsReadinessData = true;
                LastAirOps = AirOpsProjection.ProjectLifecycle(airOps.Snapshot());
                return;
            }

            var readiness = Bridge.Session.UnitReadiness;
            HasAirOpsReadinessData = readiness != null;
            if (!HasAirOpsReadinessData)
            {
                LastAirOps = Array.Empty<AirOpsEntry>();
                return;
            }

            // Only surface units the readiness map actually tracks (real airframes) —
            // UnitReadinessMap.IsReadyForLaunch defaults unknown ids to ready, so an
            // unfiltered OOB/registry sweep would show surface ships as launchable.
            IEnumerable<string> unitIds = (LastOobTree.Count > 0
                ? LastOobTree.Select(e => e.UnitId)
                : Bridge.Registry != null
                    ? Bridge.Registry.Bindings.Select(b => b.TargetId.Value)
                    : Array.Empty<string>())
                .Where(id => readiness!.IsTracked(id));

            LastAirOps = AirOpsProjection.Project(
                unitIds,
                readyForLaunch: id => readiness!.IsReadyForLaunch(id));
        }

        /// <summary>
        /// Rebuild magazine loadout rows from Session.Magazines snapshot (safe from LateUpdate).
        /// Honest empty when no magazine ledger is bound. Weapon labels use missing marker.
        /// </summary>
        public void RefreshMagazineLoadout()
        {
            if (Bridge?.Session?.Magazines == null)
            {
                HasMagazineLoadoutData = false;
                LastMagazineLoadout = Array.Empty<MagazineLoadoutEntry>();
                return;
            }

            HasMagazineLoadoutData = true;
            var snap = Bridge.Session.Magazines.Snapshot();
            if (snap.Count == 0)
            {
                LastMagazineLoadout = Array.Empty<MagazineLoadoutEntry>();
                return;
            }

            var rows = new List<(string, string, string, string, string?, int, int)>(snap.Count);
            for (var i = 0; i < snap.Count; i++)
            {
                var e = snap[i];
                rows.Add((
                    e.ShooterUnitId.ToString(),
                    string.Empty,
                    e.MountId.ToString(),
                    string.Empty,
                    null,
                    e.Remaining,
                    e.Capacity));
            }

            LastMagazineLoadout = MagazineLoadoutProjection.Project(rows);
        }

        /// <summary>
        /// Inject deck/hangar capacity states for presentation (no Session source yet).
        /// Null clears to honest NO CAPACITY DATA.
        /// </summary>
        public void SetDeckHangarFeed(IReadOnlyList<DeckHangarCapacityState>? states)
        {
            if (states is null)
            {
                HasDeckHangarData = false;
                LastDeckHangar = Array.Empty<DeckHangarCapacityEntry>();
                return;
            }

            HasDeckHangarData = true;
            LastDeckHangar = DeckHangarCapacityProjection.Project(states);
        }

        /// <summary>
        /// Rebuild deck/hangar rows from last injected feed (safe from LateUpdate).
        /// Without a feed, leaves honest empty (HasDeckHangarData false).
        /// </summary>
        public void RefreshDeckHangar()
        {
            if (!HasDeckHangarData)
            {
                LastDeckHangar = Array.Empty<DeckHangarCapacityEntry>();
            }
            // Injected rows already projected in SetDeckHangarFeed; nothing else to recompute.
        }


        /// <summary>CMD-37: take direct control of the currently selected unit.</summary>
        public bool TryTakeControlOfSelected(out string? reason)
        {
            reason = null;
            if (string.IsNullOrEmpty(SelectedUnitId))
            {
                reason = AgentDirectiveIssuance.ReasonNoSelection;
                return false;
            }

            if (!SelectedUnitHasActiveAgent())
            {
                reason = AgentDirectiveIssuance.ReasonNoAgent;
                return false;
            }

            if (!TryResolveEntityKey(SelectedUnitId, out var entityKey))
            {
                reason = "UNKNOWN_UNIT";
                return false;
            }

            if (!IsPlayerControlledUnit(entityKey))
            {
                reason = "NOT_PLAYER_CONTROLLED";
                return false;
            }

            var simTime = _lastSnapshot?.SimTime ?? 0;
            if (!Bridge.TryTakeDirectControl(entityKey, simTime))
            {
                reason = "TAKE_CONTROL_FAILED";
                return false;
            }

            RefreshAgentRoster();
            return true;
        }

        /// <summary>CMD-37: return control of the selected unit to its suspended agent.</summary>
        public bool TryReturnToAgentOfSelected(out string? reason)
        {
            reason = null;
            if (string.IsNullOrEmpty(SelectedUnitId))
            {
                reason = AgentDirectiveIssuance.ReasonNoSelection;
                return false;
            }

            if (!SelectedUnitHasSuspendedAgent())
            {
                reason = AgentDirectiveIssuance.ReasonNoAgent;
                return false;
            }

            if (!TryResolveEntityKey(SelectedUnitId, out var entityKey))
            {
                reason = "UNKNOWN_UNIT";
                return false;
            }

            if (!IsPlayerControlledUnit(entityKey))
            {
                reason = "NOT_PLAYER_CONTROLLED";
                return false;
            }

            var simTime = _lastSnapshot?.SimTime ?? 0;
            if (!Bridge.TryReleaseDirectControl(entityKey, simTime))
            {
                reason = "RETURN_TO_AGENT_FAILED";
                return false;
            }

            RefreshAgentRoster();
            return true;
        }

        /// <summary>
        /// CMD-37: validate + apply an agent directive for the selected unit.
        /// Mode changes use take/release control; hold/rtb reuse human enqueue path.
        /// </summary>
        public bool TryIssueAgentDirective(string directiveId, out string? reason)
        {
            reason = null;
            var hasSelection = !string.IsNullOrEmpty(SelectedUnitId);
            var validation = AgentDirectiveIssuance.Validate(
                directiveId,
                hasSelection,
                SelectedUnitHasActiveAgent(),
                SelectedUnitHasSuspendedAgent());
            if (!validation.Ok || validation.Request is null)
            {
                reason = validation.FailureReason ?? AgentDirectiveIssuance.ReasonUnknownDirective;
                return false;
            }

            var request = validation.Request;
            if (request.IsModeChange)
            {
                return request.Action switch
                {
                    AgentDirectiveAction.TakeControl => TryTakeControlOfSelected(out reason),
                    AgentDirectiveAction.ReturnToAgent => TryReturnToAgentOfSelected(out reason),
                    _ => FailUnknown(out reason),
                };
            }

            if (request.OrderKind is not { } kind)
            {
                reason = AgentDirectiveIssuance.ReasonUnknownDirective;
                return false;
            }

            if (!TryResolveEntityKey(SelectedUnitId!, out var entityKey))
            {
                reason = "UNKNOWN_UNIT";
                return false;
            }

            if (!IsPlayerControlledUnit(entityKey))
            {
                reason = "NOT_PLAYER_CONTROLLED";
                return false;
            }

            // hold/rtb require human controller — take control first if agent is active
            if (Bridge.Registry.TryGetBinding(entityKey, out var binding)
                && binding.Target.Slot.Active is not HumanController)
            {
                var simTimeForControl = _lastSnapshot?.SimTime ?? 0;
                if (!Bridge.TryTakeDirectControl(entityKey, simTimeForControl)
                    && binding.Target.Slot.Active is not HumanController)
                {
                    reason = "TAKE_CONTROL_FAILED";
                    return false;
                }
            }

            var simTime = _lastSnapshot?.SimTime ?? 0;
            if (!Bridge.TryEnqueueHumanOrder(entityKey, kind, simTime))
            {
                reason = "ENQUEUE_FAILED";
                return false;
            }

            return true;

            static bool FailUnknown(out string? r)
            {
                r = AgentDirectiveIssuance.ReasonUnknownDirective;
                return false;
            }
        }

        /// <summary>True when the selected unit has an active or suspended agent controller.</summary>
        public bool SelectedUnitHasAgent()
        {
            if (string.IsNullOrEmpty(SelectedUnitId) || Bridge == null)
            {
                return false;
            }

            if (!TryResolveEntityKey(SelectedUnitId, out var entityKey))
            {
                return false;
            }

            if (!Bridge.Registry.TryGetBinding(entityKey, out var binding))
            {
                return false;
            }

            var slot = binding.Target.Slot;
            return slot.Active is AgentController || slot.SuspendedAgent is not null;
        }

        /// <summary>True when the selected unit currently has an active agent controller.</summary>
        public bool SelectedUnitHasActiveAgent() => TryGetSelectedSlot(out var slot) && slot.Active is AgentController;

        /// <summary>True when the selected unit has an agent suspended by direct player control.</summary>
        public bool SelectedUnitHasSuspendedAgent() => TryGetSelectedSlot(out var slot) && slot.SuspendedAgent is not null;

        private IReadOnlyList<AgentRosterEntry> BuildAgentRosterFromRegistry()
        {
            if (Bridge?.Registry == null)
            {
                return Array.Empty<AgentRosterEntry>();
            }

            var inputs = new List<(string unitId, string controllerKind, string? agentId, string autonomy)>();
            foreach (var binding in Bridge.Registry.Bindings.OrderBy(b => b.TargetId.Value, StringComparer.Ordinal))
            {
                if (!IsPlayerControlledUnit(binding.Entity))
                {
                    continue;
                }

                var slot = binding.Target.Slot;
                var (kind, agentId, autonomy) = DescribeSlot(slot);
                inputs.Add((binding.TargetId.Value, kind, agentId, autonomy));
            }

            return AgentRosterProjection.Project(inputs);
        }

        private bool TryGetSelectedSlot(out ControllerSlot slot)
        {
            slot = null!;
            return !string.IsNullOrEmpty(SelectedUnitId)
                && TryResolveEntityKey(SelectedUnitId, out var entityKey)
                && Bridge.Registry.TryGetBinding(entityKey, out var binding)
                && (slot = binding.Target.Slot) is not null;
        }

        private bool IsPlayerControlledUnit(EntityKey entityKey)
        {
            if (!Bridge.Registry.TryGetBinding(entityKey, out var binding))
            {
                return false;
            }

            var slot = binding.Target.Slot;
            return slot.Active is HumanController
                || slot.SuspendedAgent is not null
                || slot.Active is AgentController { Id.Value: var id }
                    && id.StartsWith("friendly-", StringComparison.Ordinal);
        }

        private static (string controllerKind, string? agentId, string autonomy) DescribeSlot(ControllerSlot slot)
        {
            if (slot.Active is AgentController agent)
            {
                return ("Agent", agent.Id.Value, AgentRosterProjection.FormatAutonomyLabel(agent.Autonomy));
            }

            if (slot.Active is HumanController)
            {
                if (slot.SuspendedAgent is { } suspended)
                {
                    return ("Human", suspended.Id.Value, AgentRosterProjection.FormatAutonomyLabel(suspended.Autonomy));
                }

                return ("Human", null, "Manual");
            }

            if (slot.SuspendedAgent is { } suspendedOnly)
            {
                return ("AgentSuspended", suspendedOnly.Id.Value, AgentRosterProjection.FormatAutonomyLabel(suspendedOnly.Autonomy));
            }

            return ("None", null, "—");
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
