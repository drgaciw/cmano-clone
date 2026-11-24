// Optional Unity host — requires Plugins DLLs (see unity/ProjectAegis/README.md).
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.C2Network;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Input;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.ThreatAssessment;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;
using ProjectAegis.Delegation.C2Nodes;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Delegation.Watch;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
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

        /// <summary>Tactical map symbols (placeholder layout, pose-aware when snapshot publishes kinematics).</summary>
        public IReadOnlyList<MapSymbolEntry> LastMapSymbols { get; private set; } = Array.Empty<MapSymbolEntry>();

        /// <summary>CMD-38 plotted-course polylines (presentation-only; not Track C VFX).</summary>
        public IReadOnlyList<MapCourseOverlayEntry> LastMapCourses { get; private set; } =
            Array.Empty<MapCourseOverlayEntry>();

        /// <summary>
        /// Track C presentation-only combat VFX from the same-tick combat-event snapshot.
        /// Assigned in <see cref="RunTick"/> after <see cref="LastCombatFrame"/> is built, never inside DelegationBridge.
        /// </summary>
        public CombatVfxFrame LastCombatVfx { get; private set; } = CombatVfxFrame.Empty;

        /// <summary>Top bar labels (time, phase, score, CMD-22 Zulu/local/remain).</summary>
        public C2TopBarState LastTopBar { get; private set; } =
            new("SIM 00:00:00", "PHASE: Planning", "TIME: 1x", "MODE: —", "COMMS: NOMINAL", "SCORE: 0",
                "ZULU 00:00:00", "LOCAL 00:00:00", "REMAIN —");

        /// <summary>CMD-39 toast bind (watch pause-class + attention-tier Critical/Notable).</summary>
        public AttentionToastPresentation LastAttentionToast { get; private set; } =
            AttentionToastPresentation.Empty;

        /// <summary>Live compression chrome from the session clock (ADR-010; not a second authority).</summary>
        public string LiveCompressionLabel =>
            C2ClockCommand.FormatCompressionLabel(
                Session?.IsSimPaused ?? false,
                Session?.TimeAccelerationFactor ?? 1);

        /// <summary>Projected comms snapshot from the last <see cref="RunTick"/> refresh (CMD-32).</summary>
        public CommsStateSnapshot? LastCommsState { get; private set; }

        /// <summary>Projected C2 network-health snapshot from the last <see cref="RunTick"/> refresh (DRG-190).</summary>
        public C2NetworkHealthSnapshot? LastNetworkHealth { get; private set; }

        /// <summary>Replay-stable fingerprint for <see cref="LastNetworkHealth"/> (tick-boundary cache).</summary>
        public string? LastNetworkHealthFingerprint { get; private set; }

        /// <summary>Read-only Slice A contact frame, rebuilt once after each simulation tick.</summary>
        public SliceAContactFrame LastSliceAContacts { get; private set; } = SliceAContactFrame.Empty;

        /// <summary>Shared combat-event, explanation and assessment frame for Slice B views.</summary>
        public CombatPresentationFrame LastCombatFrame { get; private set; } = CombatPresentationFrame.Empty;

        /// <summary>Presentation-only event navigation and captured map evidence.</summary>
        public CommandReviewTimeline CommandTimeline { get; } = new();
        /// <summary>Current advisory sections; all rows are built by engine-free presenters.</summary>
        public IReadOnlyList<CommandReviewSection> LastCommandReviewSections { get; private set; } = Array.Empty<CommandReviewSection>();
        private ProjectAegis.Delegation.UnityAdapter.CommandReview.StatusFrame? _commandStatus;
        /// <summary>Current source-sequenced status changes available for related timeline navigation.</summary>
        public IReadOnlyList<StatusCorrelationRow> CommandStatusHistory =>
            _commandStatus?.Correlations ?? Array.Empty<StatusCorrelationRow>();
        private bool _commandProblemsOnly;
        private readonly List<CoordinationApprovedScope> _pendingGroupScopes = new();
        /// <summary>Current selected-contact evidence; recommendations never enqueue orders.</summary>
        public AdviceFrame? LastAdvice { get; private set; }
        private readonly SupervisedAdviceCardBinder _supervisedAdviceBinder = new SupervisedAdviceCardBinder();
        private string? _lastAdviceSubjectUnitId;
        /// <summary>Unit id used for supervised advice projection/accept (preserves selected unit across contact selection).</summary>
        public string? SupervisedAdviceUnitId =>
            !string.IsNullOrWhiteSpace(SelectedUnitId) ? SelectedUnitId : _lastAdviceSubjectUnitId;
        /// <summary>Client-local dismissed contact id for supervised advice (never mutates DecisionLog).</summary>
        public string? DismissedSupervisedAdviceContactId => _supervisedAdviceBinder.DismissedContactId;
        /// <summary>DRG-321 Jev VS3 ROE Noul gate state for the pending suggested-order card.</summary>
        public JevSuggestedOrderGateState LastJevSuggestedOrder { get; private set; } = JevSuggestedOrderGateState.Empty;
        /// <summary>Current group responsibilities and authored coverage.</summary>
        public CoordinationSnapshot LastCoordination { get; private set; } = CoordinationSnapshot.Empty;
        /// <summary>Replay-stable mission-package / C2-node snapshot (DRG-189 presentation bind).</summary>
        public MissionPackageSnapshot LastMissionPackage { get; private set; } = MissionPackageSnapshot.Empty;
        /// <summary>Presentation clutter preference for authored coverage polygons.</summary>
        public bool ShowCommandCoverage { get; set; } = true;
        /// <summary>Most recent explicit read-only skill invocation.</summary>
        public AdviceSkillResult? LastAdviceSkill { get; private set; }
        /// <summary>Runs one read-only decision-support capability against current evidence.</summary>
        public string InvokeAdviceSkill(string skillId)
        {
            if (CommandTimeline.IsInspecting) return "Return to live before requesting current advice.";
            RefreshAdvice();
            if (LastAdvice == null) return "Current evidence unavailable.";
            LastAdviceSkill = AdviceSkillService.Invoke(skillId, LastAdvice);
            return $"{skillId} | {LastAdvice.Availability} | {LastAdvice.Rationale} | {LastAdvice.Fallback}";
        }
        /// <summary>Submits deliberate human group intent through the command facade.</summary>
        public string SubmitGroupDecision(string groupId, CoordinationDecision decision)
        {
            if (CommandTimeline.IsInspecting) return "Return to live before issuing a group decision.";
            if (_pendingGroupScopes.Any(x => x.GroupId == groupId)) return "A group decision is already awaiting execution.";
            var review = LastCoordination.Groups.FirstOrDefault(x => x.Coordination.GroupId == groupId);
            var result = CoordinationCommandBridge.Submit(Bridge, _lastSnapshot, groupId, decision, review?.Intent, _pendingGroupScopes);
            if (result.ApprovedScope != null) _pendingGroupScopes.Add(result.ApprovedScope);
            return result.Accepted ? $"Queued {decision}: {string.Join(", ", result.AffectedUnitIds)}" : $"Prevented: {result.FailureReason}";
        }

        /// <summary>DRG-255: projects supervised advice card with evidence pointers and FULL_AUTONOMOUS guard.</summary>
        public SupervisedAdviceCardState ProjectSupervisedAdviceCard(
            string proposedCommandId = "fire-single",
            string? attackOptionId = "fire-single",
            bool explicitFullAutonomousLethalOptIn = false) =>
            _supervisedAdviceBinder.ProjectForUnit(
                Bridge,
                _lastSnapshot,
                LastAdvice,
                SupervisedAdviceUnitId,
                proposedCommandId,
                attackOptionId,
                explicitFullAutonomousLethalOptIn);

        /// <summary>DRG-255: validates evidence pointers, ROE, and engage gates before enqueueing a PlayerOrder.</summary>
        public SupervisedAdviceAcceptResult TryAcceptSupervisedAdvice(
            string commandId,
            string? attackOptionId = null,
            bool explicitFullAutonomousLethalOptIn = false)
        {
            var unitId = SupervisedAdviceUnitId ?? string.Empty;
            if (CommandTimeline.IsInspecting)
            {
                return new SupervisedAdviceAcceptResult(
                    false,
                    unitId,
                    LastAdvice?.ContactId ?? string.Empty,
                    null,
                    null,
                    null,
                    "REPLAY_TIMELINE_INSPECTING",
                    Array.Empty<SupervisedAdviceEvidenceLine>());
            }

            if (LastAdvice == null)
            {
                return new SupervisedAdviceAcceptResult(
                    false,
                    unitId,
                    string.Empty,
                    null,
                    null,
                    null,
                    SupervisedAdviceAcceptBridge.ReasonAdviceUnavailable,
                    Array.Empty<SupervisedAdviceEvidenceLine>());
            }

            return SupervisedAdviceAcceptBridge.TryAccept(
                Bridge,
                _lastSnapshot,
                new SupervisedAdviceAcceptRequest(
                    unitId,
                    LastAdvice,
                    commandId,
                    attackOptionId,
                    explicitFullAutonomousLethalOptIn));
        }

        /// <summary>DRG-255: client-local dismiss without mutating order log or replay fingerprint.</summary>
        public void DismissSupervisedAdvice() =>
            _supervisedAdviceBinder.Dismiss(LastAdvice?.ContactId);

        /// <summary>DRG-321: evaluates a Jev VS3 ROE Noul state payload (fail-closed, never auto-fires).</summary>
        public JevSuggestedOrderGateState EvaluateJevSuggestedOrder(string json)
        {
            LastJevSuggestedOrder = JevRoeOrderGateBinder.Evaluate(json);
            return LastJevSuggestedOrder;
        }

        /// <summary>DRG-321: dismisses the suggested-order card with zero order-log mutation.</summary>
        public void ClearJevSuggestedOrder()
        {
            LastJevSuggestedOrder = JevSuggestedOrderGateState.Empty;
        }

        /// <summary>DRG-321: routes explicit Accept click exclusively through <see cref="CoordinationCommandBridge.Submit"/>.</summary>
        public CoordinationDecisionResult TrySubmitJevSuggestedDecision()
        {
            var gate = LastJevSuggestedOrder;
            if (!gate.CardVisible || !gate.ConfirmEnabled || string.IsNullOrWhiteSpace(gate.GroupId))
            {
                return new CoordinationDecisionResult(
                    false,
                    gate.GroupId,
                    gate.Decision,
                    Array.Empty<string>(),
                    gate.BlockReason ?? JevRoeOrderGateBinder.ReasonInvalidProposal,
                    null);
            }

            if (CommandTimeline.IsInspecting)
            {
                return new CoordinationDecisionResult(
                    false,
                    gate.GroupId,
                    gate.Decision,
                    Array.Empty<string>(),
                    "REPLAY_TIMELINE_INSPECTING",
                    null);
            }

            if (_pendingGroupScopes.Any(x => x.GroupId == gate.GroupId))
            {
                return new CoordinationDecisionResult(
                    false,
                    gate.GroupId,
                    gate.Decision,
                    Array.Empty<string>(),
                    CoordinationCommandBridge.ReasonGroupDecisionPending,
                    null);
            }

            var review = LastCoordination.Groups.FirstOrDefault(x => x.Coordination.GroupId == gate.GroupId);
            var result = CoordinationCommandBridge.Submit(
                Bridge,
                _lastSnapshot,
                gate.GroupId,
                gate.Decision,
                review?.Intent,
                _pendingGroupScopes);
            if (result.ApprovedScope != null)
            {
                _pendingGroupScopes.Add(result.ApprovedScope);
            }

            if (result.Accepted)
            {
                LastJevSuggestedOrder = JevSuggestedOrderGateState.Empty;
            }

            return result;
        }
        private void RefreshAdvice()
        {
            if (Bridge != null && _lastSnapshot != null)
                LastAdvice = AdviceBridge.Build(Bridge, _lastSnapshot, LastSliceAContacts, SelectedContactId);
            RefreshCommandReviewSections();
        }
        /// <summary>Changes presentation clutter without changing status facts.</summary>
        public void SetCommandReviewProblemsOnly(bool value)
        {
            _commandProblemsOnly = value;
            RefreshCommandReviewSections();
        }
        private void RefreshCommandReviewSections() => LastCommandReviewSections =
            CommandReviewDashboard.Build(_commandStatus, LastAdvice, LastCoordination, _commandProblemsOnly);
        /// <summary>Current or inspected combat frame; never changes the simulation clock.</summary>
        public CombatPresentationFrame DisplayCombatFrame => CommandTimeline.IsInspecting ? CommandTimeline.DisplayFrame : LastCombatFrame;
        /// <summary>Historical poses only while inspecting a timeline event.</summary>
        public IReadOnlyList<MapSymbolEntry> DisplayCombatSymbols => CommandTimeline.IsInspecting ? CommandTimeline.DisplaySymbols : LastMapSymbols;
        /// <summary>One inspection identity shared by map and explanation.</summary>
        public string? DisplayCombatKey => CommandTimeline.IsInspecting ? CommandTimeline.SelectedKey : Presentation.CombatInspection.SelectedKey;
        /// <summary>Inspects a ledger event without selecting a unit for orders.</summary>
        public bool InspectTimelineEvent(ProjectAegis.Delegation.AfterAction.AfterActionLedgerEntry row) => CommandTimeline.Inspect(row);
        /// <summary>Returns map and explanation to current state.</summary>
        public void ReturnToLiveCombat()
        {
            CommandTimeline.ReturnToLive();
            Presentation.CombatInspection.Select(null);
        }
        /// <summary>Selects a live combat leg from the map/history.</summary>
        public void InspectLiveCombatEvent(string? key)
        {
            CommandTimeline.ReturnToLive();
            InspectCombatEvent(key);
        }
        /// <summary>Builds detail for the same as-of frame used by the map.</summary>
        public CombatDetailPresentation ProjectReviewCombatDetail() => CombatSelectionPresenter.Build(
            DisplayCombatFrame, DisplayCombatKey, SelectedUnitId, SelectedContactId);

        /// <summary>Inspect history without changing units selected for player commands.</summary>
        public void InspectCombatEvent(string? key) => Presentation.CombatInspection.Select(key);

        /// <summary>Read-only explanation for explicit history inspection or current unit/contact selection.</summary>
        public CombatDetailPresentation ProjectCombatDetail() => CombatSelectionPresenter.Build(
            LastCombatFrame, Presentation.CombatInspection.SelectedKey, SelectedUnitId, SelectedContactId);

        /// <summary>Sensor C2 contact list + EMCON / track indicators for HUD binding.</summary>
        public SensorC2Snapshot LastSensorC2 { get; private set; } =
            new(Array.Empty<ContactPictureEntry>(), 0, true, false, null, 0);

        // S37-04: C2 graph surfacing (dependency viewer/panel/highlights/bind) — read-only catalog projections only
        public IReadOnlyList<string> LastGraphHighlightIds { get; private set; } = Array.Empty<string>();
        public string? LastGraphLinkChainDisplay { get; private set; }

        // S37-04/05: catalog for graph/FK surfacing + overlay ranges/links (injected read-only; no bridge writes)
        public ICatalogReader? CatalogReader { get; set; }

        /// <summary>CMD-37 agent roster rows (presentation projection from registry controllers).</summary>
        public IReadOnlyList<AgentRosterEntry> LastAgentRoster { get; private set; } = Array.Empty<AgentRosterEntry>();

        /// <summary>CMD-24 Phase A air-ops readiness rows (from Session.UnitReadiness + OOB).</summary>
        public IReadOnlyList<AirOpsEntry> LastAirOps { get; private set; } = Array.Empty<AirOpsEntry>();

        /// <summary>True when Session.UnitReadiness is present (false → honest NO READINESS DATA).</summary>
        public bool HasAirOpsReadinessData { get; private set; }

        /// <summary>CMD-25 Boat Ops rows (from Session.BoatOps FSM ledger when present).</summary>
        public IReadOnlyList<BoatOpsEntry> LastBoatOps { get; private set; } = Array.Empty<BoatOpsEntry>();

        /// <summary>True when Session.BoatOps is present (false → honest NO BOAT OPS DATA).</summary>
        public bool HasBoatOpsData { get; private set; }

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
        private readonly AttentionToastBinder _attentionToast = new();
        private readonly MessageLogNotificationCoordinator _notificationCoordinator = new();
        private readonly PendingApprovalInterruptCoordinator _pendingApprovalInterrupt = new();
        private readonly Dictionary<OrderId, double?> _pendingApprovalDlzExpiry = new();
        private MessageCategoryPolicy _notificationPolicy;
        private bool _markedPreExecutionSeen;
        private ISimWorldSnapshot? _lastSnapshot;

        public DelegationBridgeHost()
        {
            _notificationPolicy = _notificationCoordinator.Policy;
        }

        /// <summary>DRG-408: active per-category message notification policy.</summary>
        public MessageCategoryPolicy NotificationPolicy => _notificationPolicy;

        /// <summary>DRG-408 / DRG-398: shared notification and safe-stop coordinator.</summary>
        public MessageLogNotificationCoordinator NotificationCoordinator => _notificationCoordinator;

        /// <summary>DRG-269: pending-approval 1x interrupt and authoritative sim-time DLZ countdown coordinator.</summary>
        public PendingApprovalInterruptCoordinator PendingApprovalInterrupt => _pendingApprovalInterrupt;

        /// <summary>DRG-269: latest evaluated pending-approval interrupt batch.</summary>
        public PendingApprovalInterruptBatch LastPendingApprovalInterrupt { get; private set; } = PendingApprovalInterruptBatch.Empty;

        /// <summary>DRG-268: latest fog-safe message-log click-through explain link.</summary>
        public MessageLogExplainLinkResult LastMessageLogExplainLink { get; private set; } = MessageLogExplainLinkResult.Empty;

        /// <summary>DRG-269: binds an authoritative DLZ expiry sim time for a pending approval order.</summary>
        public void SetPendingApprovalDlzExpiry(OrderId orderId, double? expiresAtSimTime)
        {
            if (expiresAtSimTime.HasValue)
            {
                _pendingApprovalDlzExpiry[orderId] = expiresAtSimTime.Value;
            }
            else
            {
                _pendingApprovalDlzExpiry.Remove(orderId);
            }
        }

        /// <summary>DRG-269: evaluates pending approvals and applies a single-shot 1x drop via C2ClockCommand.</summary>
        public PendingApprovalInterruptBatch EvaluatePendingApprovalInterrupts()
        {
            if (Bridge?.Orchestrator == null)
            {
                LastPendingApprovalInterrupt = PendingApprovalInterruptBatch.Empty;
                return LastPendingApprovalInterrupt;
            }

            var isObserver = ObserverModeChromePresentation.Project(Bridge).IsObserver;
            var currentSimTime = _lastSnapshot?.SimTime ?? 0.0;
            var batch = _pendingApprovalInterrupt.Evaluate(
                Bridge.Orchestrator.PendingApprovals,
                currentSimTime,
                _pendingApprovalDlzExpiry,
                IsReplayViewerAttached,
                isObserver);
            LastPendingApprovalInterrupt = batch;

            if (batch.ShouldDropToRealTime && Session != null)
            {
                C2ClockCommand.TrySetAcceleration(Session, 1, out _);
                SyncTimeCompressionLabel();
            }

            return batch;
        }

        /// <summary>DRG-269: validates replay/observer/stale/DLZ-expiry before approving a pending order.</summary>
        public bool TryApprovePendingOrderValidated(OrderId orderId, out PendingApprovalDecisionValidation validation)
        {
            var isObserver = Bridge != null && ObserverModeChromePresentation.Project(Bridge).IsObserver;
            var currentSimTime = _lastSnapshot?.SimTime ?? 0.0;
            _pendingApprovalDlzExpiry.TryGetValue(orderId, out var expiry);

            validation = _pendingApprovalInterrupt.ValidateDecision(
                orderId,
                Bridge?.Orchestrator?.PendingApprovals,
                currentSimTime,
                expiry,
                IsReplayViewerAttached,
                isObserver);
            if (!validation.Allowed || Bridge?.Orchestrator == null)
            {
                return false;
            }

            var ok = Bridge.Orchestrator.TryApprovePendingOrder(orderId);
            if (ok)
            {
                _pendingApprovalDlzExpiry.Remove(orderId);
                EvaluatePendingApprovalInterrupts();
            }

            return ok;
        }

        /// <summary>DRG-269: validates replay/observer/stale before rejecting a pending order.</summary>
        public bool TryRejectPendingOrderValidated(OrderId orderId, out PendingApprovalDecisionValidation validation)
        {
            var isObserver = Bridge != null && ObserverModeChromePresentation.Project(Bridge).IsObserver;
            var currentSimTime = _lastSnapshot?.SimTime ?? 0.0;
            validation = _pendingApprovalInterrupt.ValidateDecision(
                orderId,
                Bridge?.Orchestrator?.PendingApprovals,
                currentSimTime,
                expiresAtSimTime: null,
                IsReplayViewerAttached,
                isObserver);
            if (!validation.Allowed || Bridge?.Orchestrator == null)
            {
                return false;
            }

            var ok = Bridge.Orchestrator.TryRejectPendingOrder(orderId);
            if (ok)
            {
                _pendingApprovalDlzExpiry.Remove(orderId);
                EvaluatePendingApprovalInterrupts();
            }

            return ok;
        }

        /// <summary>DRG-268: fog-safe message-log row click-through selecting visible unit/contact and populating EngageExplain.</summary>
        public MessageLogExplainLinkResult SelectMessageLogDeepLink(DecisionLogDeepLinkDetail? detail)
        {
            if (detail == null || !detail.Found)
            {
                LastMessageLogExplainLink = MessageLogExplainLinkResult.Empty;
                return LastMessageLogExplainLink;
            }

            var visibleFriendlies = new HashSet<string>(StringComparer.Ordinal);
            if (LastOobTree != null)
            {
                for (var i = 0; i < LastOobTree.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(LastOobTree[i].UnitId))
                    {
                        visibleFriendlies.Add(LastOobTree[i].UnitId);
                    }
                }
            }

            var visibleContacts = new HashSet<string>(StringComparer.Ordinal);
            if (LastSliceAContacts != null)
            {
                foreach (var key in LastSliceAContacts.Authorities.Keys)
                {
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        visibleContacts.Add(key);
                    }
                }
            }

            if (_lastSnapshot?.PrimaryHostileContactId is { } primaryHostile && !string.IsNullOrWhiteSpace(primaryHostile.Value))
            {
                visibleContacts.Add(primaryHostile.Value);
            }

            var resolved = MessageLogExplainDeepLinkCoordinator.Resolve(detail, visibleFriendlies, visibleContacts);
            LastMessageLogExplainLink = resolved;

            if (!resolved.SuppressedByFog && !string.IsNullOrWhiteSpace(resolved.SafeUnitIdToSelect))
            {
                SelectUnit(resolved.SafeUnitIdToSelect);
            }

            return resolved;
        }

        /// <summary>DRG-408 / DRG-398: evaluation outcome from the latest live message log batch.</summary>
        public NotificationBatchEvaluation LastNotificationBatch { get; private set; } = NotificationBatchEvaluation.Empty;

        /// <summary>DRG-408: active map balloon alerts projected from player-visible logs.</summary>
        public IReadOnlyList<MapBalloonAlert> ActiveMapBalloons => _notificationCoordinator.ActiveBalloons;

        /// <summary>DRG-408: active (non-dismissed) message notification alerts, including non-pause balloons.</summary>
        public IReadOnlyList<MessageNotificationAlert> ActiveMessageAlerts => _notificationCoordinator.ActiveAlerts;

        /// <summary>DRG-408: unresolved pause-class message alerts gating resume.</summary>
        public IReadOnlyList<MessageNotificationAlert> UnresolvedMessageAlerts => _notificationCoordinator.UnresolvedPauseAlerts;

        /// <summary>DRG-408 / DRG-398: true while any pause-class message alert remains unacknowledged.</summary>
        public bool HasUnresolvedNotificationPause => _notificationCoordinator.HasUnresolvedPause;

        /// <summary>Latest live simulation tick for presentation staleness calculations.</summary>
        public ulong CurrentSimTick => (ulong)Math.Max(0, (long)(_lastSnapshot?.SimTime ?? 0));

        private void Awake()
        {
            Bridge = new DelegationBridge(
                globalSeed,
                mvpEngagement: enableMvpEngagement,
                scenarioPolicyId: scenarioPolicyId);
            // Presentation-only bind (DRG-162): overlay hosts need catalog links/ranges.
            // Prefer the session catalog when MVP engage is on; otherwise the same factory the bridge uses.
            CatalogReader ??= Bridge.Session?.CatalogReader
                ?? CatalogReaderFactory.TryCreateBalticPatrolReader()
                ?? InMemoryCatalogReader.BalticPatrolFixture();
            LastMissionList = MissionListBridge.ProjectFrom(Bridge.Orchestrator.ScenarioPolicy?.MissionTimeline);
            // Restore UI-local chrome collapse bag (in-memory; no DecisionLog / file I/O).
            ChromeCollapse = _chromePrefs.Restore(C2ChromeCollapseState.Expanded);
        }

        /// <summary>
        /// Bind an engine-free player mission. The mission remains the only owner of stepping;
        /// this host publishes its snapshots to the existing C2 presentation surfaces.
        /// </summary>
        public void BindExternalSession(DelegationBridge bridge, int seed)
        {
            Bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            globalSeed = seed;
            CatalogReader = Bridge.Session?.CatalogReader;
            _lastSnapshot = null;
            _markedPreExecutionSeen = false;
            Presentation.SelectFriendlyUnit("u1");
            LastMissionList = MissionListBridge.ProjectFrom(Bridge.Orchestrator.ScenarioPolicy?.MissionTimeline);
        }

        /// <summary>
        /// Refresh an already-stepped world without ticking it a second time. Mission score
        /// is objective-based; legacy kill/denial tallies remain confined to smoke fixtures.
        /// </summary>
        public void RefreshExternalPresentation(
            ISimWorldSnapshot snapshot, int missionScore, double remainingSeconds, string missionStatus,
            IReadOnlyList<MapSymbolEntry>? knownSymbols = null,
            IReadOnlyList<MessageLogLine>? observedMessages = null)
        {
            RefreshPresentation(snapshot, evaluateNotifications: false);
            // The legacy combined order log contains both sides' decisions. Player mission
            // views publish only their explicitly scoped picture and observable messages.
            LastMapSymbols = knownSymbols ?? Array.Empty<MapSymbolEntry>();
            LastOobTree = LastOobTree.Where(unit => LastMapSymbols.Any(symbol =>
                symbol.Affiliation == "Friendly" && symbol.SymbolId == unit.UnitId)).ToArray();
            LastMessageLog = observedMessages ?? Array.Empty<MessageLogLine>();
            EvaluateMessageNotifications();
            LastCombatDomainActivityTags = CombatDomainActivityTags.FromMessageLog(LastMessageLog);
            LastCombatVfx = CombatVfxFrame.Empty;
            LastSensorC2 = LastSensorC2 with
            {
                Contacts = LastSensorC2.Contacts.Where(contact =>
                    contact.TargetId == snapshot.PrimaryHostileContactId?.Value).ToArray(),
            };
            LastTopBar = LastTopBar with
            {
                ScoreLabel = $"MISSION SCORE: {missionScore}",
                PhaseLabel = missionStatus,
                CompressionLabel = timeCompressionLabel,
                RemainingDurationLabel = $"REMAIN {TimeSpan.FromSeconds(Math.Max(0, remainingSeconds)):mm\\:ss}",
            };
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

        /// <summary>CMD-04: set session acceleration; label follows the sim clock.</summary>
        public bool TrySetTimeAcceleration(int factor, out string? reason)
        {
            var ok = C2ClockCommand.TrySetAcceleration(Session, factor, out reason);
            if (ok)
            {
                SyncTimeCompressionLabel();
            }

            return ok;
        }

        /// <summary>CMD-39: pause via session clock (WatchAutoPauseGate callers use the same API).</summary>
        public bool TryPauseSim(out string? reason)
        {
            var ok = C2ClockCommand.TryPause(Session, out reason);
            if (ok)
            {
                SyncTimeCompressionLabel();
            }

            return ok;
        }

        /// <summary>CMD-39: gated resume — unresolved pause-class cards and message alerts block unless override.</summary>
        public bool TryResumeSim(bool explicitOverride, out string? reason)
        {
            var ok = C2ClockCommand.TryResume(Session, _notificationCoordinator, explicitOverride, out reason);
            if (ok)
            {
                SyncTimeCompressionLabel();
                RefreshAttentionToast();
            }

            return ok;
        }

        /// <summary>Re-project toast from DecisionLog attention + session watch queue + message alerts (not Tick).</summary>
        public AttentionToastPresentation RefreshAttentionToast()
        {
            if (Bridge == null)
            {
                LastAttentionToast = AttentionToastPresentation.Empty;
                return LastAttentionToast;
            }

            var baseToast = _attentionToast.Refresh(
                Bridge.Orchestrator.DecisionLog,
                Session?.WatchQueue,
                Session?.WatchPauseGate);
            LastAttentionToast = AttentionToastApplyState.MergeMessageNotifications(
                baseToast,
                _notificationCoordinator.UnresolvedPauseAlerts,
                _notificationCoordinator.CanResume(),
                _notificationCoordinator.HasUnresolvedPause);
            return LastAttentionToast;
        }

        public bool TryAcknowledgeAttentionToast(string cardId)
        {
            if (!string.IsNullOrEmpty(cardId)
                && cardId.StartsWith("msglog:", StringComparison.Ordinal)
                && ulong.TryParse(cardId.Substring("msglog:".Length), out var seqId))
            {
                return TryAcknowledgeMessageAlert(seqId);
            }

            if (!_attentionToast.TryAcknowledge(cardId, Session?.WatchQueue))
            {
                return false;
            }

            RefreshAttentionToast();
            return true;
        }

        public bool TryDismissAttentionToast(string cardId)
        {
            if (!string.IsNullOrEmpty(cardId)
                && cardId.StartsWith("msglog:", StringComparison.Ordinal)
                && ulong.TryParse(cardId.Substring("msglog:".Length), out var seqId))
            {
                return TryDismissMessageAlert(seqId);
            }

            if (!_attentionToast.TryDismiss(cardId, Session?.WatchQueue))
            {
                return false;
            }

            RefreshAttentionToast();
            return true;
        }

        /// <summary>DRG-408 / DRG-398: re-project live message log and evaluate safe-stop / map balloons.</summary>
        public void RefreshMessageNotifications()
        {
            if (Bridge == null)
            {
                return;
            }

            LastMessageLog = MessageLogBridge.ProjectLive(Bridge);
            EvaluateMessageNotifications();
        }

        /// <summary>DRG-408: update one category's notification rule (blocked when replay viewer attached).</summary>
        public bool SetMessageCategoryRule(string category, MessageCategoryRule rule)
        {
            if (IsReplayViewerAttached || string.IsNullOrWhiteSpace(category))
            {
                return false;
            }

            _notificationPolicy = _notificationPolicy.WithRule(category, rule);
            _notificationCoordinator.UpdatePolicy(_notificationPolicy);
            return true;
        }

        /// <summary>DRG-408: restore default notification policy rules (blocked when replay viewer attached).</summary>
        public bool ResetMessageCategoryPolicyDefaults()
        {
            if (IsReplayViewerAttached)
            {
                return false;
            }

            _notificationPolicy = MessageCategoryPolicy.LoadDefault();
            _notificationCoordinator.UpdatePolicy(_notificationPolicy);
            return true;
        }

        /// <summary>DRG-408: acknowledge one pause-class message alert without clearing other unresolved alerts.</summary>
        public bool TryAcknowledgeMessageAlert(ulong sequenceId)
        {
            if (IsReplayViewerAttached || !_notificationCoordinator.TryAcknowledgeAlert(sequenceId))
            {
                return false;
            }

            RefreshAttentionToast();
            return true;
        }

        /// <summary>DRG-408: dismiss one alert/balloon visually without clearing other unresolved pauses.</summary>
        public bool TryDismissMessageAlert(ulong sequenceId)
        {
            if (IsReplayViewerAttached || !_notificationCoordinator.TryDismissAlert(sequenceId))
            {
                return false;
            }

            RefreshAttentionToast();
            return true;
        }

        /// <summary>DRG-273: non-mutating live DecisionLog deep-link lookup for a message-log SequenceId.</summary>
        public DecisionLogDeepLinkDetail InspectMessageLogDecision(ulong sequenceId)
        {
            if (Bridge == null)
            {
                return DecisionLogDeepLinkDetail.NotFound(sequenceId);
            }

            return MessageLogBridge.InspectDecisionLogLive(Bridge, sequenceId);
        }

        private void EvaluateMessageNotifications()
        {
            if (!_markedPreExecutionSeen && Bridge != null && Bridge.Phase == SimulationPhase.Executing)
            {
                _markedPreExecutionSeen = true;
                ulong transitionSeqId = 0;
                for (var i = 0; i < LastMessageLog.Count; i++)
                {
                    var line = LastMessageLog[i];
                    if (string.Equals(line.Category, "MODE", StringComparison.Ordinal)
                        && line.Text.Contains("Planning → Executing", StringComparison.Ordinal))
                    {
                        transitionSeqId = line.SequenceId;
                        break;
                    }
                }

                if (transitionSeqId > 0)
                {
                    var preExecution = new List<MessageLogLine>();
                    for (var i = 0; i < LastMessageLog.Count; i++)
                    {
                        if (LastMessageLog[i].SequenceId <= transitionSeqId)
                        {
                            preExecution.Add(LastMessageLog[i]);
                        }
                    }

                    _notificationCoordinator.MarkExistingAsSeen(preExecution);
                }
            }

            LastNotificationBatch = C2ClockCommand.EvaluateAndApplyNotificationStop(
                Session,
                _notificationCoordinator,
                LastMessageLog,
                IsReplayViewerAttached,
                out _);
            SyncTimeCompressionLabel();
            RefreshAttentionToast();
        }

        /// <summary>
        /// Play Mode demo: one pause-class hostile contact so the toast host has a real event.
        /// Idempotent — no-ops when the watch queue already has cards.
        /// </summary>
        public bool TrySeedDemoWatchAttention()
        {
            if (Session == null || Session.WatchQueue.Cards.Count > 0)
            {
                return Session != null && Session.WatchQueue.Cards.Count > 0;
            }

            Session.ReportWatchAttention(new WatchAttentionEvent(
                "watch:demo:hostile-1",
                WatchAttentionKind.HostileOrUnknownContact,
                WatchAttentionPriority.Critical,
                1,
                "hostile-1",
                ReasonDetail: "Play Mode demo contact"));
            SyncTimeCompressionLabel();
            RefreshAttentionToast();
            return true;
        }

        private void SyncTimeCompressionLabel()
        {
            if (Session == null)
            {
                return;
            }

            timeCompressionLabel = Session.IsSimPaused
                ? "PAUSED"
                : Session.TimeAccelerationFactor + "x";
        }

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

        /// <summary>
        /// CMD-11 live feed: project engage-explain for the currently selected unit.
        /// Returns <see cref="EngageExplain.Empty"/> when no selection or snapshot is available.
        /// </summary>
        public EngageExplain ProjectSelectedEngageExplain()
        {
            if (_lastSnapshot == null || string.IsNullOrEmpty(SelectedUnitId))
            {
                return EngageExplain.Empty;
            }

            var preview = ProjectSelectedEngagePreview();
            return EngageExplainProjection.Project(preview);
        }

        /// <summary>CMD-11 / DRG-266: live engage preview for weapon-panel DLZ binders.</summary>
        public EngagePreview? ProjectSelectedEngagePreview()
        {
            if (_lastSnapshot == null || string.IsNullOrEmpty(SelectedUnitId))
            {
                return null;
            }

            return Bridge.GetEngagePreviewForUnit(SelectedUnitId, _lastSnapshot);
        }

        /// <summary>DRG-266: structured threat-range facts for contact-hover DLZ when supplied by runtime evidence.</summary>
        public ThreatRangeAssessment? ProjectSelectedContactThreatRange()
        {
            if (_lastSnapshot == null || string.IsNullOrEmpty(SelectedContactId))
            {
                return null;
            }

            if (_lastSnapshot is IAdviceEvidenceSource source
                && source.TryGetAdviceEvidence(SelectedContactId, out var evidence))
            {
                return evidence.ThreatAssessment?.Range;
            }

            return null;
        }

        /// <summary>DRG-259: WRA salvo remaining vs max for weapon-panel chrome (advisory only).</summary>
        public WraSalvoRemainingState ProjectSelectedWraSalvoRemaining()
        {
            if (_lastSnapshot == null || string.IsNullOrEmpty(SelectedUnitId))
            {
                return WraSalvoRemainingState.Empty;
            }

            var policyProfile = Bridge.Orchestrator.ScenarioPolicy;
            var policy = policyProfile?.ResolveUnitPolicy(SelectedUnitId, isFriendly: true).Effective
                ?? EffectivePolicy.DefaultFree;
            var ctx = BuildSelectedLiveEngageContext(SelectedUnitId);
            return WraSalvoRemainingBinder.Bind(
                SelectedUnitId,
                in ctx,
                policy,
                ProjectSelectedEngagePreview());
        }

        /// <summary>
        /// DRG-167: battle graphic for the displayed combat frame.
        /// Projection read only — does not tick, enqueue, or write the order log.
        /// </summary>
        public BattleGraphicState ProjectBattleGraphic(
            IReadOnlyList<MapSymbolEntry> symbols,
            CombatZoomBand zoom = CombatZoomBand.Tactical,
            string? selectedKey = null) =>
            BattleGraphicBinder.Bind(DisplayCombatFrame, symbols, zoom, selectedKey);

        /// <summary>Read-only live engage context mirror of <see cref="DelegationBridge.GetEngagePreviewForUnit"/> inputs.</summary>
        private EngageContext BuildSelectedLiveEngageContext(string shooterUnitId)
        {
            var engageDefaults = Bridge.Orchestrator.ScenarioPolicy?.EngageDefaults
                ?? ScenarioEngageDefaults.MvpFallback;
            var ctx = engageDefaults.ToEngageContext(engageDefaults.DefaultMagazineRounds);
            if (_lastSnapshot == null)
            {
                return ctx;
            }

            var airReady = Session?.UnitReadiness?.IsReadyForLaunch(shooterUnitId) ?? true;
            return ctx with
            {
                HasFireControlTrack = _lastSnapshot.HasFireControlTrackOnPrimaryContact,
                RadarEmconActive = _lastSnapshot.ObserverRadarEmconActive,
                AirOperationsReady = airReady,
            };
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

            // UAT D22: refuse before EnsureHumanControl so a hostile AI unit is never taken over.
            if (PlayerSideGuard.IsOpposingAiUnit(Bridge, entityKey))
            {
                failureReason = ProjectAegis.Delegation.UnityAdapter.Bridge.C2PlayerCommandBridge.ReasonNotPlayerControlled;
                return false;
            }

            EnsureHumanControl(entityKey);
            return PlayerSideGuard.TryEnqueueAttackOption(Bridge, entityKey, optionId, _lastSnapshot, out failureReason);
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

        /// <summary>
        /// CMD-25: issue a launch/recover/abort command for an explicit craft id. Boat Ops manages
        /// its own row selection independent of the C2 unit-selection system (<see cref="SelectUnit"/>),
        /// so this resolves the target entity directly rather than via <see cref="SelectedUnitId"/>.
        /// </summary>
        public bool TryIssueBoatCommand(string craftId, string commandId, out string? reason)
        {
            reason = null;
            if (string.IsNullOrEmpty(craftId))
            {
                reason = ProjectAegis.Delegation.Input.C2CommandIssuance.ReasonNoSelection;
                return false;
            }

            if (!TryResolveEntityKey(craftId, out var entityKey))
            {
                reason = ProjectAegis.Delegation.UnityAdapter.Bridge.C2PlayerCommandBridge.ReasonUnknownUnit;
                return false;
            }

            if (PlayerSideGuard.IsOpposingAiUnit(Bridge, entityKey))
            {
                reason = ProjectAegis.Delegation.UnityAdapter.Bridge.C2PlayerCommandBridge.ReasonNotPlayerControlled;
                return false;
            }

            var simTime = _lastSnapshot?.SimTime ?? 0;
            return Bridge.TryIssuePlayerCommand(entityKey, commandId, simTime, out reason);
        }

        /// <summary>CMD-25 / LOG-09: launch the given craft (order-log path; FSM advances on next session tick).</summary>
        public bool TryLaunchBoat(string craftId, out string? reason) =>
            TryIssueBoatCommand(craftId, "launch_boat", out reason);

        /// <summary>CMD-25 / LOG-10: recover the given craft (order-log path).</summary>
        public bool TryRecoverBoat(string craftId, out string? reason) =>
            TryIssueBoatCommand(craftId, "recover_boat", out reason);

        /// <summary>CMD-25 / LOG-11: abort launch for the given craft (order-log path).</summary>
        public bool TryAbortBoatLaunch(string craftId, out string? reason) =>
            TryIssueBoatCommand(craftId, "abort_boat_launch", out reason);

        public void SelectUnit(string unitId)
        {
            _lastAdviceSubjectUnitId = string.IsNullOrWhiteSpace(unitId) ? null : unitId;
            Presentation.SelectFriendlyUnit(unitId);
            RefreshSelectionPresentation();
            ApplyGraphSurfacingForSelection();  // S37-04 C2 graph surfacing (highlights/bind)
        }

        public void SelectContact(string contactId)
        {
            _supervisedAdviceBinder.ClearDismiss();
            var contacts = ContactPictureProjection.Project(Bridge.Orchestrator.DecisionLog);
            Presentation.SelectHostileContact(contactId, contacts);
            RefreshSelectionPresentation();
            RefreshAdvice();
            // contacts do not drive platform graph
        }

        /// <summary>
        /// Call once per sim step after building snapshot and sink for this frame.
        /// </summary>
        public DelegationTickResult RunTick(ISimWorldSnapshot snapshot, IOrderSink sink)
        {
            _lastSnapshot = snapshot;
            var groupSink = new CoordinationOrderSink(Bridge.Registry, snapshot, sink, _pendingGroupScopes);
            var result = Bridge.Tick(snapshot, groupSink);
            _pendingGroupScopes.RemoveAll(x => groupSink.ConsumedScopeIds.Contains(x.ScopeId));
            var expiredScopes = CoordinationScopeReconciler.FindInvalidOrExpired(Bridge, _pendingGroupScopes, CurrentSimTick);
            _pendingGroupScopes.RemoveAll(x => expiredScopes.Contains(x.ScopeId));
            RefreshPresentation(snapshot);
            return result;
        }

        private void RefreshPresentation(ISimWorldSnapshot snapshot, bool evaluateNotifications = true)
        {
            _lastSnapshot = snapshot;
            LastMessageLog = MessageLogBridge.ProjectLive(Bridge);
            if (evaluateNotifications)
            {
                EvaluateMessageNotifications();
            }
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
            LastMapCourses = MapPictureBridge.BuildCourses(
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
            LastCommsState = CommsStateProjection.Project(Bridge.Orchestrator.DecisionLog);
            LastNetworkHealth = C2NetworkHealthBridge.Build(
                Bridge.Orchestrator.DecisionLog,
                Bridge.Registry,
                snapshot,
                CatalogReader,
                CurrentSimTick);
            LastNetworkHealthFingerprint = LastNetworkHealth == null
                ? "c2net:empty"
                : C2NetworkHealthFingerprint.Compute(LastNetworkHealth);
            LastSliceAContacts = SliceAContactFrameBridge.Build(snapshot, Bridge, CatalogReader);
            LastCombatFrame = CombatPresentationFrameBridge.Build(
                Bridge.Orchestrator.DecisionLog, LastSliceAContacts, snapshot.SimTime);
            LastCombatVfx = CombatVfxFrameBridge.Build(LastCombatFrame, LastMapSymbols);
            CommandTimeline.Capture(LastCombatFrame, LastMapSymbols);
            _commandStatus = ProjectAegis.Delegation.UnityAdapter.CommandReview.StatusFrameBridge.Build(Bridge, snapshot, LastSliceAContacts);
            LastCoordination = CoordinationBridge.Build(Bridge, snapshot);
            LastMissionPackage = MissionPackagePresentationSource.Project(
                Bridge,
                snapshot,
                snapshot is ICoordinationFacts coordinationFacts
                    ? coordinationFacts.Packages
                    : Array.Empty<PackageDefinition>());
            RefreshAdvice();
            // CMD-37: additive roster projection (no Tick body rewrite)
            LastAgentRoster = BuildAgentRosterFromRegistry();
            // CMD-24 Phase A: additive air-ops readiness projection
            RefreshAirOps();
            // CMD-25: additive boat-ops projection (no Tick body rewrite)
            RefreshBoatOps();
            // CMD-24 Wave4: magazine + deck/hangar feeds (no Tick body rewrite)
            RefreshMagazineLoadout();
            RefreshDeckHangar();
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
        /// Rebuild CMD-25 Boat Ops rows from Session.BoatOps FSM ledger (safe from LateUpdate).
        /// Honest empty when no boat-ops map is bound yet (feature only activates once a
        /// LaunchBoat/RecoverBoat/AbortBoatLaunch order has been processed at least once).
        /// </summary>
        public void RefreshBoatOps()
        {
            var boatOps = Bridge?.Session?.BoatOps;
            if (boatOps == null)
            {
                HasBoatOpsData = false;
                LastBoatOps = Array.Empty<BoatOpsEntry>();
                return;
            }

            HasBoatOpsData = true;
            LastBoatOps = BoatOpsProjection.Project(boatOps.Snapshot(), boatOps.SeaStateDouglas);
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


        /// <summary>DRG-247: project control-handoff and agent pause/resume state for the selected unit.</summary>
        public RightUnitControlState ProjectSelectedControlHandoff() =>
            RightUnitControlProjection.Project(Bridge, SelectedUnitId);

        /// <summary>DRG-272: true when a replay viewer is attached (read-only UI gate).</summary>
        public bool IsReplayViewerAttached => Bridge != null && Bridge.AttachReplayViewer;

        /// <summary>DRG-272: operator reclassify override for the selected contact (order-log backed).</summary>
        public bool TryReclassifySelectedContact(string affiliation, out string? reason)
        {
            var result = ContactOverrideCommand.TryReclassify(
                Bridge,
                SelectedContactId,
                affiliation,
                _lastSnapshot?.SimTime ?? 0);
            reason = result.Accepted ? result.Message : (result.ReasonCode ?? ContactOverrideCommand.ReasonUnknownContact);
            if (result.Accepted)
            {
                RefreshContactOverridePresentation();
            }

            return result.Accepted;
        }

        /// <summary>DRG-272: operator drop contact override for the selected contact (order-log backed).</summary>
        public bool TryDropSelectedContact(out string? reason)
        {
            var result = ContactOverrideCommand.TryDrop(
                Bridge,
                SelectedContactId,
                _lastSnapshot?.SimTime ?? 0);
            reason = result.Accepted ? result.Message : (result.ReasonCode ?? ContactOverrideCommand.ReasonUnknownContact);
            if (result.Accepted)
            {
                RefreshContactOverridePresentation();
            }

            return result.Accepted;
        }

        private void RefreshContactOverridePresentation()
        {
            if (Bridge == null)
            {
                return;
            }

            if (_lastSnapshot != null)
            {
                LastSensorC2 = SensorC2Bridge.Build(_lastSnapshot, Bridge.Orchestrator.DecisionLog);
                LastMapSymbols = MapPictureBridge.Build(
                    _lastSnapshot,
                    Bridge.Registry,
                    Bridge.Orchestrator.DecisionLog,
                    globalSeed);
                LastSliceAContacts = SliceAContactFrameBridge.Build(_lastSnapshot, Bridge, CatalogReader);
            }
            else
            {
                LastSliceAContacts = LastSliceAContacts with
                {
                    ContactOverrides = ContactOverrideProjection.Project(Bridge.Orchestrator.DecisionLog),
                    IsReplayReadOnly = Bridge.AttachReplayViewer,
                };
            }

            LastMessageLog = MessageLogBridge.ProjectLive(Bridge);
        }

        /// <summary>CMD-37 / DRG-247: take direct control of the currently selected unit.</summary>
        public bool TryTakeControlOfSelected(out string? reason)
        {
            reason = null;
            if (string.IsNullOrEmpty(SelectedUnitId))
            {
                reason = AgentDirectiveIssuance.ReasonNoSelection;
                return false;
            }

            if (!TryResolveEntityKey(SelectedUnitId, out var entityKey))
            {
                reason = C2PlayerCommandBridge.ReasonUnknownUnit;
                return false;
            }

            var simTime = _lastSnapshot?.SimTime ?? 0;
            if (!C2PlayerCommandBridge.TryTakeControl(Bridge, entityKey, simTime, out reason))
            {
                return false;
            }

            RefreshAgentRoster();
            RefreshSelectionPresentation();
            return true;
        }

        /// <summary>CMD-37 / DRG-247: return control of the selected unit to its suspended agent or parent group.</summary>
        public bool TryReturnToAgentOfSelected(out string? reason)
        {
            reason = null;
            if (string.IsNullOrEmpty(SelectedUnitId))
            {
                reason = AgentDirectiveIssuance.ReasonNoSelection;
                return false;
            }

            if (!TryResolveEntityKey(SelectedUnitId, out var entityKey))
            {
                reason = C2PlayerCommandBridge.ReasonUnknownUnit;
                return false;
            }

            var simTime = _lastSnapshot?.SimTime ?? 0;
            if (!C2PlayerCommandBridge.TryReturnToAgent(Bridge, entityKey, simTime, out reason))
            {
                return false;
            }

            RefreshAgentRoster();
            RefreshSelectionPresentation();
            return true;
        }

        /// <summary>DRG-247: pause autonomous agent actions for the selected unit without clearing tasking.</summary>
        public bool TryPauseAgentOfSelected(out string? reason)
        {
            reason = null;
            if (string.IsNullOrEmpty(SelectedUnitId))
            {
                reason = AgentDirectiveIssuance.ReasonNoSelection;
                return false;
            }

            if (!TryResolveEntityKey(SelectedUnitId, out var entityKey))
            {
                reason = C2PlayerCommandBridge.ReasonUnknownUnit;
                return false;
            }

            var simTime = _lastSnapshot?.SimTime ?? 0;
            if (!C2PlayerCommandBridge.TryPauseAgentActions(Bridge, entityKey, simTime, out reason))
            {
                return false;
            }

            RefreshAgentRoster();
            RefreshSelectionPresentation();
            return true;
        }

        /// <summary>DRG-247: resume suspended autonomous agent actions for the selected unit.</summary>
        public bool TryResumeAgentOfSelected(out string? reason)
        {
            reason = null;
            if (string.IsNullOrEmpty(SelectedUnitId))
            {
                reason = AgentDirectiveIssuance.ReasonNoSelection;
                return false;
            }

            if (!TryResolveEntityKey(SelectedUnitId, out var entityKey))
            {
                reason = C2PlayerCommandBridge.ReasonUnknownUnit;
                return false;
            }

            var simTime = _lastSnapshot?.SimTime ?? 0;
            if (!C2PlayerCommandBridge.TryResumeAgentActions(Bridge, entityKey, simTime, out reason))
            {
                return false;
            }

            RefreshAgentRoster();
            RefreshSelectionPresentation();
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
                SelectedUnitHasSuspendedAgent(),
                SelectedUnitIsAgentPaused());
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
                    AgentDirectiveAction.PauseAgent => TryPauseAgentOfSelected(out reason),
                    AgentDirectiveAction.ResumeAgent => TryResumeAgentOfSelected(out reason),
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

        /// <summary>True when the selected unit currently has a paused active agent controller.</summary>
        public bool SelectedUnitIsAgentPaused() =>
            TryGetSelectedSlot(out var slot) && slot.Active is AgentController agent && agent.IsActionsPaused;

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

        private bool IsPlayerControlledUnit(EntityKey entityKey) =>
            PlayerSideGuard.IsPlayerControlledUnit(Bridge, entityKey);

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
