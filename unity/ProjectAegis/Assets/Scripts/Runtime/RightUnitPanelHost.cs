// Doc-20 right unit detail panel — UI Toolkit bound to DelegationBridgeHost.
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class RightUnitPanelHost : MonoBehaviour
    {
        private const string RootName = "unit-detail-root";
        private const string UnitIdName = "unit-id-line";
        private const string StatusName = "status-line";
        private const string CommsName = "comms-line";
        private const string MagazineName = "magazine-line";
        private const string EmconName = "emcon-line";
        private const string DoctrineName = "doctrine-line";
        private const string FuelName = "fuel-line";
        private const string DlzName = "dlz-line";
        private const string WraSalvoName = "wra-salvo-line";
        private const string EngageName = "engage-line";
        private const string AttackOptionsName = "attack-options-line";
        private const string CommitStripConstraintsName = CommitConstraintStripBinder.ConstraintElementName;
        private const string CommitStripCostName = CommitConstraintStripBinder.CostElementName;
        private const string CommitBarkName = CommitRefusalBarkBinder.BarkElementName;
        private const string ContactName = "contact-line";

        /// <summary>Repo-relative Specced production USS for ASSET-008 (not Approved).</summary>
        public const string SpeccedProductionUssRelativePath =
            ProjectAegis.Delegation.Projection.SpeccedC2PanelStylePaths.Asset008RightUnitDetailUss;

        /// <summary>Unity-side USS path constant for host style-path parity (S106).</summary>
        public const string UnityUssRelativePath =
            ProjectAegis.Delegation.Projection.SpeccedC2PanelStylePaths.UnityRightUnitDetailUss;

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _unitIdLine;
        private Label? _statusLine;
        private Label? _controlStatusLine;
        private Button? _takeControlButton;
        private Button? _returnToAgentButton;
        private Button? _pauseAgentButton;
        private Button? _resumeAgentButton;
        private Label? _controlFeedbackLine;
        private Label? _commsLine;
        private Label? _magazineLine;
        private Label? _emconLine;
        private Label? _doctrineLine;
        private Label? _fuelLine;
        private Label? _dlzLine;
        private Label? _wraSalvoLine;
        private Label? _engageLine;
        private Label? _attackOptionsLine;
        private Label? _commitStripConstraintsLine;
        private Label? _commitStripCostLine;
        private Label? _commitBarkLine;
        private readonly Dictionary<string, Button> _attackButtons = new();
        private readonly Dictionary<string, VisualElement> _weaponRows = new();
        private Label? _contactLine;
        private bool _wired;
        private bool _attackHandlersRegistered;
        private bool _controlHandlersRegistered;
        private string? _commandFeedback;
        private string? _feedbackUnitId;
        private string? _controlFeedback;
        private string? _controlFeedbackUnitId;
        private RightUnitControlState _controlState = RightUnitControlState.Empty;
        private string? _feedbackBaseAttackOptionsLine;
        private string? _attackOptionsDisplay;
        private string _armedCommitOptionId = WeaponAbortTooltipBinder.FireSingleOptionId;
        private UnitDetailPresentation _presentation = UnitDetailPresentation.Empty;
        private UnitDetailFuelBandPresentation _fuelBandPresentation = new("FUEL: —", null, FuelBandCueClasses.Unknown, null);
        private DlzLiveSurfaceState _dlzSurface = DlzLiveSurfaceState.Empty;
        private WraSalvoRemainingState _wraSalvoSurface = WraSalvoRemainingState.Empty;
        private AttackOptionsPreviewState _attackPreview = AttackOptionsPreviewState.Empty;
        private WeaponAbortTooltipState _weaponAbort = WeaponAbortTooltipState.Empty;
        private CommitConstraintStripState _commitStrip = CommitConstraintStripState.Empty;
        private CommitRefusalBark? _commitBark;
        private CommitRefusalBark? _refusedCommitBark;
        private string? _refusedBarkUnitId;
        private string? _commitBarkProjectionUnitId;
        private int _commitBarkProjectionEntryCount = -1;
        private ulong _commitBarkProjectionLastSequenceId;
        private readonly RightUnitCombatDetailCache _combatDetailCache = new();
        private readonly RuntimeTooltipPresenter _tooltipPresenter = new();

        /// <summary>Last applied unit-detail presentation (S107 apply-state).</summary>
        public UnitDetailPresentation LastPresentation => _presentation;

        /// <summary>Last applied weapon-panel DLZ row (DRG-266).</summary>
        public DlzLiveSurfaceState LastDlzSurface => _dlzSurface;

        /// <summary>Last applied weapon-panel WRA salvo row (DRG-259).</summary>
        public WraSalvoRemainingState LastWraSalvoSurface => _wraSalvoSurface;

        /// <summary>Last applied attack-options preview (DRG-262). Presentation only.</summary>
        public AttackOptionsPreviewState LastAttackOptionsPreview => _attackPreview;

        /// <summary>Last applied weapon-abort tooltip (DRG-258). Presentation only.</summary>
        public WeaponAbortTooltipState LastWeaponAbortTooltip => _weaponAbort;

        /// <summary>Last applied pre-commit constraint + magazine cost strip (DRG-370). Presentation only.</summary>
        public CommitConstraintStripState LastCommitStrip => _commitStrip;

        /// <summary>Last applied commit refusal or order-log drop bark (DRG-370). Presentation only.</summary>
        public CommitRefusalBark? LastCommitBark => _commitBark;

        /// <summary>Play Mode runtime tooltip presenter wired onto this panel (DRG-351).</summary>
        public RuntimeTooltipPresenter TooltipPresenter => _tooltipPresenter;

        /// <summary>Last projected control-handoff and agent pause/resume state (DRG-247).</summary>
        public RightUnitControlState LastControlState => _controlState;

        /// <summary>Last control-handoff / pause-resume feedback text (DRG-247).</summary>
        public string? LastControlFeedback => _controlFeedback;

        private void Reset()
        {
            if (bridgeHost == null)
            {
                bridgeHost = GetComponent<DelegationBridgeHost>();
            }

            _document = GetComponent<UIDocument>();
            UiDocumentPanelSettingsBootstrap.EnsureDocument(_document);
        }

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            if (panelAsset != null)
            {
                _document.visualTreeAsset = panelAsset;
            }

            if (panelStyles != null && _document.rootVisualElement != null)
            {
                _document.rootVisualElement.styleSheets.Add(panelStyles);
            }
        }

        private void OnEnable()
        {
            TryWireElements();
            Refresh();
        }

        private void OnDisable()
        {
            _tooltipPresenter.Detach();
        }

        private void OnDestroy()
        {
            _tooltipPresenter.Detach();
        }

        private void LateUpdate()
        {
            if (!showPanel || bridgeHost == null)
            {
                return;
            }

            if (!_wired)
            {
                TryWireElements();
            }

            Refresh();
        }

        private void TryWireElements()
        {
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            var panel = root.Q<VisualElement>(RootName) ?? root;
            _tooltipPresenter.Attach(panel);
            _unitIdLine = panel.Q<Label>(UnitIdName);
            _statusLine = panel.Q<Label>(StatusName);
            _controlStatusLine = panel.Q<Label>("unit-control-status");
            _takeControlButton = panel.Q<Button>("unit-take-control");
            _returnToAgentButton = panel.Q<Button>("unit-return-to-agent");
            _pauseAgentButton = panel.Q<Button>("unit-pause-agent");
            _resumeAgentButton = panel.Q<Button>("unit-resume-agent");
            _controlFeedbackLine = panel.Q<Label>("unit-control-feedback");
            _commsLine = panel.Q<Label>(CommsName);
            _magazineLine = panel.Q<Label>(MagazineName);
            _emconLine = panel.Q<Label>(EmconName);
            _doctrineLine = panel.Q<Label>(DoctrineName);
            _fuelLine = panel.Q<Label>(FuelName);
            _dlzLine = panel.Q<Label>(DlzName);
            _wraSalvoLine = panel.Q<Label>(WraSalvoName);
            _engageLine = panel.Q<Label>(EngageName);
            _attackOptionsLine = panel.Q<Label>(AttackOptionsName);
            if (_attackOptionsLine != null)
            {
                _attackOptionsLine.style.whiteSpace = WhiteSpace.Normal;
            }
            _commitStripConstraintsLine = panel.Q<Label>(CommitStripConstraintsName);
            _commitStripCostLine = panel.Q<Label>(CommitStripCostName);
            _commitBarkLine = panel.Q<Label>(CommitBarkName);
            _contactLine = panel.Q<Label>(ContactName);
            _attackButtons.Clear();
            _weaponRows.Clear();
            RegisterAttackButton(panel.Q<Button>("attack-fire-single"), "fire-single");
            RegisterAttackButton(panel.Q<Button>("attack-fire-salvo"), "fire-salvo");
            RegisterAttackButton(panel.Q<Button>("attack-hold-fire"), "hold-fire");
            RegisterWeaponRow(panel.Q<VisualElement>(WeaponAbortTooltipBinder.FireSingleRowName), WeaponAbortTooltipBinder.FireSingleOptionId);
            RegisterWeaponRow(panel.Q<VisualElement>(WeaponAbortTooltipBinder.FireSalvoRowName), WeaponAbortTooltipBinder.FireSalvoOptionId);
            _wired = _unitIdLine != null && _statusLine != null && _magazineLine != null &&
                     _emconLine != null && _doctrineLine != null;

            WireAttackMenuHandlers();
            WireControlHandoffHandlers();

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }
        }

        private void RegisterAttackButton(Button? button, string optionId)
        {
            if (button == null)
            {
                return;
            }

            _attackButtons[optionId] = button;
            _tooltipPresenter.Observe(button);
        }

        private void RegisterWeaponRow(VisualElement? row, string optionId)
        {
            if (row == null)
            {
                return;
            }

            row.focusable = true;
            _weaponRows[optionId] = row;
            _tooltipPresenter.Observe(row);
        }

        /// <summary>Apply panel state via shipped binder + apply-state path (S107).</summary>
        public void ApplyPanelState(UnitDetailPanelState? state)
        {
            _presentation = UnitDetailApplyState.Apply(state);
            _attackPreview = AttackOptionsPreviewBinder.Bind(state?.AttackMenu);
            _weaponAbort = WeaponAbortTooltipState.Empty;
            _commitStrip = CommitConstraintStripState.Empty;
            _commitBark = null;
            _refusedCommitBark = null;
            _refusedBarkUnitId = null;
            ApplyPresentationToLabels();

            // Mirror Refresh(): without this, direct-apply callers keep whatever
            // fire/hold buttons the previous bridge Refresh() left displayed and
            // enabled. A null/empty menu correctly hides them.
            RefreshAttackMenuButtons();
            _tooltipPresenter.RefreshActive();
        }

        private void Refresh()
        {
            var selectedUnitId = bridgeHost == null ? null : bridgeHost.SelectedUnitId;
            if (_commandFeedback != null &&
                !string.Equals(_feedbackUnitId, selectedUnitId, System.StringComparison.Ordinal))
            {
                _commandFeedback = null;
                _feedbackUnitId = null;
                _feedbackBaseAttackOptionsLine = null;
                _attackOptionsDisplay = null;
            }

            if (_refusedCommitBark != null &&
                !string.Equals(_refusedBarkUnitId, selectedUnitId, System.StringComparison.Ordinal))
            {
                _refusedCommitBark = null;
                _refusedBarkUnitId = null;
            }

            if (_controlFeedback != null &&
                !string.Equals(_controlFeedbackUnitId, selectedUnitId, System.StringComparison.Ordinal))
            {
                _controlFeedback = null;
                _controlFeedbackUnitId = null;
            }

            if (!_wired || bridgeHost == null)
            {
                return;
            }

            var state = UnitDetailPanelBinder.Bind(
                bridgeHost.LastUnitDetail,
                bridgeHost.Presentation.ResolveContactLine());
            _presentation = UnitDetailApplyState.Apply(state);
            _attackPreview = AttackOptionsPreviewBinder.Bind(state.AttackMenu);
            var engagePreview = bridgeHost.ProjectSelectedEngagePreview();
            _dlzSurface = DlzLiveSurfaceBinder.BindFromEngagePreview(engagePreview);
            // Selected-unit scope, not the combat-history inspection key (DRG-352).
            // DRG-407: reuse combat detail and the engage explanation while the
            // preview, combat content, unit, and contact are unchanged.
            var combatFrame = bridgeHost.LastCombatFrame;
            var selectedContactId = bridgeHost.SelectedContactId;
            if (!_combatDetailCache.TryGet(
                    engagePreview,
                    combatFrame,
                    selectedUnitId,
                    selectedContactId,
                    out var cachedCombat))
            {
                var explain = EngageExplainProjection.Project(engagePreview);
                var detail = WeaponAbortTooltipBinder.ScopeCombatDetail(
                    combatFrame, selectedUnitId, selectedContactId);
                cachedCombat = _combatDetailCache.Remember(
                    engagePreview,
                    combatFrame,
                    selectedUnitId,
                    selectedContactId,
                    explain,
                    detail,
                    WeaponAbortTooltipBinder.Bind(explain, detail));
            }

            _weaponAbort = cachedCombat.Tooltip;
            _wraSalvoSurface = bridgeHost.ProjectSelectedWraSalvoRemaining();
            _commitStrip = RightUnitCommitProjection.ProjectStrip(
                bridgeHost.Bridge,
                selectedUnitId,
                engagePreview,
                _attackPreview,
                _wraSalvoSurface,
                _weaponAbort,
                _armedCommitOptionId);
            _commitBark = _refusedCommitBark ?? ProjectLatestDroppedBarkCached(selectedUnitId);
            _controlState = bridgeHost.ProjectSelectedControlHandoff();
            ApplyPresentationToLabels();
            RefreshAttackMenuButtons();
            RefreshControlHandoffButtons();
            _tooltipPresenter.RefreshActive();

            var root = _document.rootVisualElement?.Q(RootName);
            if (root != null)
            {
                root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void ApplyPresentationToLabels()
        {
            if (_unitIdLine != null)
            {
                _unitIdLine.text = _presentation.UnitIdLine;
            }

            if (_statusLine != null)
            {
                _statusLine.text = _presentation.StatusLine;
            }

            // CMD-17: null-safe — older UXML without comms-line still wires.
            if (_commsLine != null)
            {
                _commsLine.text = _presentation.CommsLine;
            }

            if (_magazineLine != null)
            {
                _magazineLine.text = _presentation.MagazineLine;
            }

            if (_emconLine != null)
            {
                _emconLine.text = _presentation.EmconLine;
            }

            if (_doctrineLine != null)
            {
                _doctrineLine.text = _presentation.DoctrineLine;
            }

            if (_fuelLine != null)
            {
                _fuelBandPresentation = UnitDetailFuelBandBinder.Bind(_presentation);
                _fuelLine.text = string.IsNullOrEmpty(_fuelBandPresentation.DeclutterToken)
                    ? _fuelBandPresentation.FuelLineText
                    : $"{_fuelBandPresentation.FuelLineText} {_fuelBandPresentation.DeclutterToken}";
                ApplyFuelBandCueClass(_fuelLine, _fuelBandPresentation.CueClass);
            }

            foreach (var row in DlzLiveSurfacePanelBinder.BindRows(_dlzSurface))
            {
                ApplyDlzSurfaceRow(_dlzLine, row.Text, row.CueClass);
            }

            foreach (var row in WraSalvoRemainingPanelBinder.BindRows(_wraSalvoSurface))
            {
                var wraSalvoText = string.IsNullOrEmpty(_wraSalvoSurface.DeclutterToken)
                    ? row.Text
                    : $"{row.Text} {_wraSalvoSurface.DeclutterToken}";
                ApplyWraSalvoSurfaceRow(_wraSalvoLine, wraSalvoText, row.CueClass);
            }

            if (_engageLine != null)
            {
                _engageLine.text = _presentation.EngagePreviewLine;
            }

            if (_attackOptionsLine != null)
            {
                ApplyAttackOptionsCue(_attackOptionsLine, _attackPreview.CueClass);
                var previewLine = _attackPreview.PreviewLine;
                if (string.IsNullOrEmpty(_commandFeedback))
                {
                    _attackOptionsDisplay = previewLine;
                }
                else if (!string.Equals(
                             _feedbackBaseAttackOptionsLine,
                             previewLine,
                             System.StringComparison.Ordinal))
                {
                    _feedbackBaseAttackOptionsLine = previewLine;
                    _attackOptionsDisplay = $"{previewLine}\n{_commandFeedback}";
                }

                if (!string.Equals(_attackOptionsLine.text, _attackOptionsDisplay, System.StringComparison.Ordinal))
                {
                    _attackOptionsLine.text = _attackOptionsDisplay;
                }
            }

            ApplyCommitStripRows();
            ApplyCommitBarkRow();

            if (_contactLine != null)
            {
                _contactLine.text = _presentation.ContactLine;
            }

            if (_controlStatusLine != null)
            {
                _controlStatusLine.text = _controlState.StatusLabel;
            }

            if (_controlFeedbackLine != null)
            {
                _controlFeedbackLine.text = _controlState.IsDisabled && !string.IsNullOrEmpty(_controlState.DisabledReason)
                    ? _controlState.DisabledReason
                    : (_controlFeedback ?? string.Empty);
            }
        }

        private void ApplyCommitStripRows()
        {
            foreach (var row in CommitConstraintStripBinder.BindRows(_commitStrip))
            {
                if (string.Equals(row.ElementName, CommitStripConstraintsName, System.StringComparison.Ordinal))
                {
                    ApplyCueSurfaceRow(_commitStripConstraintsLine, row.Text, row.CueClass, CommitConstraintStripCueClasses.All);
                }
                else if (string.Equals(row.ElementName, CommitStripCostName, System.StringComparison.Ordinal))
                {
                    ApplyCueSurfaceRow(_commitStripCostLine, row.Text, row.CueClass, CommitConstraintStripCueClasses.All);
                }
            }
        }

        private void ApplyCommitBarkRow()
        {
            if (_commitBarkLine == null)
            {
                return;
            }

            if (_commitBark == null)
            {
                ApplyCueSurfaceRow(_commitBarkLine, string.Empty, string.Empty, CommitRefusalBarkCueClasses.All);
                _commitBarkLine.style.display = DisplayStyle.None;
                return;
            }

            var row = CommitRefusalBarkBinder.BindRow(_commitBark);
            ApplyCueSurfaceRow(_commitBarkLine, row.Text, row.CueClass, CommitRefusalBarkCueClasses.All);
            _commitBarkLine.style.display = DisplayStyle.Flex;
        }

        private CommitRefusalBark? ProjectLatestDroppedBarkCached(string? selectedUnitId)
        {
            if (bridgeHost?.Bridge is null || string.IsNullOrWhiteSpace(selectedUnitId))
            {
                _commitBarkProjectionUnitId = null;
                _commitBarkProjectionEntryCount = -1;
                _commitBarkProjectionLastSequenceId = 0;
                return null;
            }

            var entries = bridgeHost.Bridge.Orchestrator.DecisionLog.ChronologicalEntries();
            var entryCount = entries.Count;
            var lastSequenceId = entryCount == 0 ? 0 : entries[entryCount - 1].SequenceId;
            if (!string.Equals(_commitBarkProjectionUnitId, selectedUnitId, System.StringComparison.Ordinal)
                || _commitBarkProjectionEntryCount != entryCount
                || _commitBarkProjectionLastSequenceId != lastSequenceId)
            {
                _commitBark = RightUnitCommitProjection.ProjectLatestDroppedBark(bridgeHost.Bridge, selectedUnitId);
                _commitBarkProjectionUnitId = selectedUnitId;
                _commitBarkProjectionEntryCount = entryCount;
                _commitBarkProjectionLastSequenceId = lastSequenceId;
            }

            return _commitBark;
        }

        private void RefreshAttackMenuButtons()
        {
            foreach (var (optionId, button) in _attackButtons)
            {
                var row = AttackOptionsPreviewBinder.FindRow(_attackPreview, optionId);
                if (row == null)
                {
                    button.style.display = DisplayStyle.None;
                    ApplyWeaponRowChrome(optionId, visible: false, tooltip: null, denied: false);
                    continue;
                }

                button.style.display = DisplayStyle.Flex;
                var salvoBlocked = optionId == "fire-salvo" && _wraSalvoSurface.IsExhausted;
                var enabled = row.Enabled && !salvoBlocked;
                string text;
                string tooltip;
                if (!row.Enabled)
                {
                    text = row.ButtonText;
                    tooltip = row.AbortReason ?? AttackOptionsPreviewBinder.MissingAbortToken;
                }
                else if (salvoBlocked)
                {
                    var reason = _wraSalvoSurface.AbortReasonCode ?? "WRA salvo exhausted";
                    text = $"{row.Label} — {reason}";
                    tooltip = reason;
                }
                else
                {
                    text = row.Label;
                    tooltip = row.Label;
                }

                if (!enabled
                    && WeaponAbortTooltipBinder.AppliesTo(optionId)
                    && _weaponAbort.IsDenied
                    && !string.IsNullOrEmpty(_weaponAbort.TooltipText))
                {
                    tooltip = _weaponAbort.TooltipText;
                }

                button.text = text;
                button.tooltip = tooltip;
                button.SetEnabled(enabled);
                ApplyWeaponRowChrome(optionId, visible: true, tooltip, denied: !enabled && _weaponAbort.IsDenied);
                ApplyAttackOptionsCue(
                    button,
                    enabled ? AttackOptionsCueClasses.Ready : AttackOptionsCueClasses.Blocked);
            }
        }

        private void WireAttackMenuHandlers()
        {
            if (_attackHandlersRegistered)
            {
                return;
            }

            foreach (var (optionId, button) in _attackButtons)
            {
                var capturedId = optionId;
                button.clicked += () => OnAttackOptionClicked(capturedId);
                button.RegisterCallback<PointerEnterEvent>(_ => ArmCommitOption(capturedId));
                button.RegisterCallback<FocusEvent>(_ => ArmCommitOption(capturedId));
            }

            foreach (var (optionId, row) in _weaponRows)
            {
                var capturedId = optionId;
                row.RegisterCallback<PointerEnterEvent>(_ => ArmCommitOption(capturedId));
                row.RegisterCallback<FocusEvent>(_ => ArmCommitOption(capturedId));
            }

            if (_attackButtons.Count > 0)
            {
                _attackHandlersRegistered = true;
            }
        }

        private void WireControlHandoffHandlers()
        {
            if (_controlHandlersRegistered)
            {
                return;
            }

            var wiredAny = false;
            if (_takeControlButton != null)
            {
                _takeControlButton.clicked += OnTakeControlClicked;
                wiredAny = true;
            }

            if (_returnToAgentButton != null)
            {
                _returnToAgentButton.clicked += OnReturnToAgentClicked;
                wiredAny = true;
            }

            if (_pauseAgentButton != null)
            {
                _pauseAgentButton.clicked += OnPauseAgentClicked;
                wiredAny = true;
            }

            if (_resumeAgentButton != null)
            {
                _resumeAgentButton.clicked += OnResumeAgentClicked;
                wiredAny = true;
            }

            if (wiredAny)
            {
                _controlHandlersRegistered = true;
            }
        }

        private void RefreshControlHandoffButtons()
        {
            var disabledReason = _controlState.IsDisabled ? (_controlState.DisabledReason ?? string.Empty) : null;
            ApplyControlButton(_takeControlButton, _controlState.CanTakeControl, disabledReason ?? "Take direct control");
            ApplyControlButton(_returnToAgentButton, _controlState.CanReturnToAgent, disabledReason ?? "Return control to agent");
            ApplyControlButton(_pauseAgentButton, _controlState.CanPauseAgent, disabledReason ?? "Pause autonomous agent actions");
            ApplyControlButton(_resumeAgentButton, _controlState.CanResumeAgent, disabledReason ?? "Resume autonomous agent actions");
        }

        private static void ApplyControlButton(Button? button, bool enabled, string tooltip)
        {
            if (button == null)
            {
                return;
            }

            button.SetEnabled(enabled);
            button.tooltip = tooltip;
        }

        private void OnTakeControlClicked() =>
            ExecuteControlAction("Take Control", () => bridgeHost.TryTakeControlOfSelected(out var r) ? (true, r) : (false, r));

        private void OnReturnToAgentClicked() =>
            ExecuteControlAction("Return to Agent", () => bridgeHost.TryReturnToAgentOfSelected(out var r) ? (true, r) : (false, r));

        private void OnPauseAgentClicked() =>
            ExecuteControlAction("Pause Agent", () => bridgeHost.TryPauseAgentOfSelected(out var r) ? (true, r) : (false, r));

        private void OnResumeAgentClicked() =>
            ExecuteControlAction("Resume Agent", () => bridgeHost.TryResumeAgentOfSelected(out var r) ? (true, r) : (false, r));

        private void ExecuteControlAction(string label, System.Func<(bool Ok, string? Reason)> action)
        {
            if (bridgeHost == null)
            {
                _controlFeedback = "DENIED: NO_BRIDGE";
                _controlFeedbackUnitId = null;
                ApplyPresentationToLabels();
                return;
            }

            _controlFeedbackUnitId = bridgeHost.SelectedUnitId;
            var (ok, reason) = action();
            _controlFeedback = ok
                ? $"APPLIED: {label}"
                : $"DENIED: {DescribeFailure(string.IsNullOrEmpty(reason) ? "CONTROL_HANDOFF_FAILED" : reason)} ({reason})";
            Refresh();
        }

        private void ArmCommitOption(string optionId)
        {
            if (string.Equals(_armedCommitOptionId, optionId, System.StringComparison.Ordinal))
            {
                return;
            }

            _armedCommitOptionId = optionId;
            Refresh();
        }

        private void OnAttackOptionClicked(string optionId)
        {
            _armedCommitOptionId = optionId;
            if (bridgeHost == null)
            {
                SetCommandFeedback(optionId, false, "NO_BRIDGE");
                return;
            }

            var queued = bridgeHost.TrySelectAttackOption(optionId, out var failureReason);
            SetCommandFeedback(optionId, queued, failureReason);
            Refresh();
        }

        private void SetCommandFeedback(string optionId, bool queued, string? failureReason)
        {
            var unitId = bridgeHost == null ? null : bridgeHost.SelectedUnitId;
            _feedbackUnitId = unitId;
            if (queued)
            {
                _commandFeedback = $"QUEUED: {ResolveAttackOptionLabel(optionId)}";
                _refusedCommitBark = null;
                _refusedBarkUnitId = null;
            }
            else
            {
                var reason = string.IsNullOrEmpty(failureReason) ? "ISSUE_FAILED" : failureReason;
                _commandFeedback = $"DENIED: {DescribeFailure(reason)} ({reason})";
                var simTime = bridgeHost != null ? (double)bridgeHost.CurrentSimTick : 0d;
                _refusedCommitBark = CommitRefusalBarkBinder.Refused(unitId, optionId, failureReason, simTime);
                _refusedBarkUnitId = unitId;
                _commitBark = _refusedCommitBark;
            }

            _feedbackBaseAttackOptionsLine = null;
            ApplyPresentationToLabels();
        }

        private string ResolveAttackOptionLabel(string optionId)
        {
            var row = AttackOptionsPreviewBinder.FindRow(_attackPreview, optionId);
            if (row != null)
            {
                return row.Label;
            }

            return optionId switch
            {
                "fire-single" => "Fire 1 round",
                "fire-salvo" => "Fire salvo",
                "hold-fire" => "Hold fire",
                _ => optionId,
            };
        }

        private void ApplyWeaponRowChrome(string optionId, bool visible, string? tooltip, bool denied)
        {
            if (!_weaponRows.TryGetValue(optionId, out var weaponRow) || weaponRow == null)
            {
                return;
            }

            weaponRow.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            // Disabled buttons are not pickable. The row keeps hover and focus for the abort tooltip.
            weaponRow.tooltip = tooltip;
            weaponRow.focusable = true;
            if (denied)
            {
                weaponRow.AddToClassList(WeaponAbortTooltipBinder.DeniedRowClass);
            }
            else
            {
                weaponRow.RemoveFromClassList(WeaponAbortTooltipBinder.DeniedRowClass);
            }
        }

        private static void ApplyAttackOptionsCue(VisualElement element, string cueClass)
        {
            foreach (var knownCue in AttackOptionsCueClasses.All)
            {
                element.RemoveFromClassList(knownCue);
            }

            if (!string.IsNullOrEmpty(cueClass))
            {
                element.AddToClassList(cueClass);
            }
        }

        private static string DescribeFailure(string reason)
        {
            if (reason == "UNKNOWN_OPTION") return "Unknown command";
            if (reason == "NO_SELECTION") return "No unit selected";
            if (reason == "UNKNOWN_UNIT") return "Unknown unit";
            if (reason == "NO_BRIDGE") return "Command service unavailable";
            if (reason == "ISSUE_FAILED") return "Command was not queued";

            var words = reason.Replace('_', ' ').ToLowerInvariant();
            return words.Length == 0 ? "Command blocked" : char.ToUpperInvariant(words[0]) + words.Substring(1);
        }

        private static void ApplyFuelBandCueClass(Label label, string cueClass)
        {
            foreach (var knownCue in FuelBandCueClasses.All)
            {
                label.RemoveFromClassList(knownCue);
            }

            if (!string.IsNullOrEmpty(cueClass))
            {
                label.AddToClassList(cueClass);
            }
        }

        private static void ApplyDlzSurfaceRow(Label? label, string text, string cueClass)
        {
            ApplyCueSurfaceRow(label, text, cueClass, DlzCueClasses.All);
        }

        private static void ApplyWraSalvoSurfaceRow(Label? label, string text, string cueClass)
        {
            ApplyCueSurfaceRow(label, text, cueClass, WraSalvoCueClasses.All);
        }

        private static void ApplyCueSurfaceRow(Label? label, string text, string cueClass, IReadOnlyList<string> knownCues)
        {
            if (label == null)
            {
                return;
            }

            label.text = text;
            foreach (var knownCue in knownCues)
            {
                label.RemoveFromClassList(knownCue);
            }

            if (!string.IsNullOrEmpty(cueClass))
            {
                label.AddToClassList(cueClass);
            }
        }
    }
}
#endif
