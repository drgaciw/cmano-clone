// DRG-175: Formation and task-group combat coordination host — ADR-010 §2-3, ADR-007, ADR-001.
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class CoordinationPanelHost : MonoBehaviour
    {
        public const int DefaultSortingOrder = 16;

        private static readonly string[] StatusClasses =
        {
            "coord-status--ok",
            "coord-status--degraded",
            "coord-status--blocked",
            "coord-status--advisory",
        };

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private VisualElement? _root;
        private Label? _headerTitle;
        private Label? _advisoryBadge;
        private DropdownField? _groupDropdown;
        private Label? _intentLine;
        private Label? _controllerLine;
        private VisualElement? _boundariesList;
        private VisualElement? _effectsList;
        private Label? _blockReasonLine;
        private Button? _holdButton;
        private Button? _withdrawButton;
        private Button? _reattackButton;
        private VisualElement? _assistedConfirmBox;
        private Label? _assistedConfirmTitle;
        private Label? _assistedConfirmBody;
        private Button? _assistedConfirmAccept;
        private Button? _assistedConfirmCancel;
        private Label? _feedbackLine;

        private readonly List<Label> _boundaryRows = new List<Label>();
        private readonly List<Label> _effectRows = new List<Label>();
        private readonly List<string> _groupChoices = new List<string>();

        private CoordinationPresentationState _lastPresentation = CoordinationPresentationState.Empty;
        private CoordinationSnapshot? _lastCoordination;
        private ulong _lastSimTick;
        private bool _lastInspecting;
        private bool _lastReplayAttached;
        private bool _lastShowPanel;
        private string? _selectedGroupId;
        private string? _lastSelectedGroupId;
        private string? _lastFingerprint;
        private string? _lastFeedback;
        private bool _wired;

        /// <summary>Last projected coordination presentation (ADR-010).</summary>
        public CoordinationPresentationState LastPresentation => _lastPresentation;

        /// <summary>Currently selected task-group identifier.</summary>
        public string? SelectedGroupId => _selectedGroupId;

        /// <summary>Latest command or validation feedback message.</summary>
        public string? LastFeedback => _lastFeedback;

        private void Reset()
        {
            if (bridgeHost == null)
            {
                bridgeHost = GetComponent<DelegationBridgeHost>();
            }

            _document = GetComponent<UIDocument>();
            UiDocumentPanelSettingsBootstrap.EnsureDocument(_document);
            _document.sortingOrder = DefaultSortingOrder;
        }

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            UiDocumentPanelSettingsBootstrap.EnsureDocument(_document);
            _document.sortingOrder = DefaultSortingOrder;

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
            _wired = false;
            _lastCoordination = null;
            _lastFingerprint = null;
            TryWireElements();
            Refresh();
        }

        private void OnDisable()
        {
            UnwireElements();
            CoordinationAssistedWithdrawRedeployConfirmBinder.TryCancel();
            HideAssistedConfirm();
            _wired = false;
        }

        private void LateUpdate()
        {
            if (!_wired)
            {
                TryWireElements();
            }

            Refresh();
        }

        /// <summary>Selects a specific task group for display and decision routing.</summary>
        public void SelectGroup(string? groupId)
        {
            if (string.Equals(_selectedGroupId, groupId, StringComparison.Ordinal))
            {
                return;
            }

            _selectedGroupId = groupId;
            _lastSelectedGroupId = null;
            CoordinationAssistedWithdrawRedeployConfirmBinder.TryCancel();
            HideAssistedConfirm();
            Refresh();
        }

        /// <summary>Applies a projected presentation state directly (headless / test path).</summary>
        public void Apply(CoordinationPresentationState presentation)
        {
            _lastPresentation = presentation ?? CoordinationPresentationState.Empty;
            _lastCoordination = null;
            _lastFingerprint = _lastPresentation.Fingerprint;
            ApplyPresentation(_lastPresentation);
        }

        /// <summary>Forces an immediate read-only refresh from <see cref="DelegationBridgeHost"/>.</summary>
        public void Refresh()
        {
            if (!_wired)
            {
                return;
            }

            if (_root != null)
            {
                _root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (!showPanel || bridgeHost == null)
            {
                _lastShowPanel = showPanel;
                return;
            }

            var coordination = bridgeHost.LastCoordination;
            var simTick = bridgeHost.CurrentSimTick;
            var isInspecting = bridgeHost.CommandTimeline.IsInspecting;
            var isReplay = bridgeHost.IsReplayViewerAttached;

            if (ReferenceEquals(coordination, _lastCoordination)
                && _lastSimTick == simTick
                && _lastInspecting == isInspecting
                && _lastReplayAttached == isReplay
                && _lastShowPanel == showPanel
                && string.Equals(_selectedGroupId, _lastSelectedGroupId, StringComparison.Ordinal))
            {
                return;
            }

            _lastCoordination = coordination;
            _lastSimTick = simTick;
            _lastInspecting = isInspecting;
            _lastReplayAttached = isReplay;
            _lastShowPanel = showPanel;

            var state = CoordinationPresentation.Build(
                coordination,
                bridgeHost.Bridge,
                world: null,
                isTimelineInspecting: isInspecting,
                hasPendingScope: null);

            _lastPresentation = state;
            if (string.Equals(_lastFingerprint, state.Fingerprint, StringComparison.Ordinal)
                && string.Equals(_selectedGroupId, _lastSelectedGroupId, StringComparison.Ordinal))
            {
                return;
            }

            _lastFingerprint = state.Fingerprint;
            ApplyPresentation(state);
            _lastSelectedGroupId = _selectedGroupId;
        }

        private void TryWireElements()
        {
            if (_document == null)
            {
                return;
            }

            var docRoot = _document.rootVisualElement;
            if (docRoot == null)
            {
                return;
            }

            UnwireElements();

            _root = docRoot.Q<VisualElement>("coordination-panel-root") ?? docRoot;
            _headerTitle = _root.Q<Label>("coord-header-title");
            _advisoryBadge = _root.Q<Label>("coord-advisory-badge");
            _groupDropdown = _root.Q<DropdownField>("coord-group-dropdown");
            _intentLine = _root.Q<Label>("coord-intent-line");
            _controllerLine = _root.Q<Label>("coord-controller-line");
            _boundariesList = _root.Q<VisualElement>("coord-boundaries-list");
            _effectsList = _root.Q<VisualElement>("coord-effects-list");
            _blockReasonLine = _root.Q<Label>("coord-block-reason-line");
            _holdButton = _root.Q<Button>("coord-hold-button");
            _withdrawButton = _root.Q<Button>("coord-withdraw-button");
            _reattackButton = _root.Q<Button>("coord-reattack-button");
            _assistedConfirmBox = _root.Q<VisualElement>("coord-assisted-confirm-box");
            _assistedConfirmTitle = _root.Q<Label>("coord-assisted-confirm-title");
            _assistedConfirmBody = _root.Q<Label>("coord-assisted-confirm-body");
            _assistedConfirmAccept = _root.Q<Button>("coord-assisted-confirm-accept");
            _assistedConfirmCancel = _root.Q<Button>("coord-assisted-confirm-cancel");
            _feedbackLine = _root.Q<Label>("coord-feedback-line");

            if (_groupDropdown != null)
            {
                _groupDropdown.UnregisterValueChangedCallback(OnGroupDropdownChanged);
                _groupDropdown.RegisterValueChangedCallback(OnGroupDropdownChanged);
            }

            if (_holdButton != null)
            {
                _holdButton.clicked -= OnHoldClicked;
                _holdButton.clicked += OnHoldClicked;
            }

            if (_withdrawButton != null)
            {
                _withdrawButton.clicked -= OnWithdrawClicked;
                _withdrawButton.clicked += OnWithdrawClicked;
            }

            if (_reattackButton != null)
            {
                _reattackButton.clicked -= OnReattackClicked;
                _reattackButton.clicked += OnReattackClicked;
            }

            if (_assistedConfirmAccept != null)
            {
                _assistedConfirmAccept.clicked -= OnAssistedConfirmAccept;
                _assistedConfirmAccept.clicked += OnAssistedConfirmAccept;
            }

            if (_assistedConfirmCancel != null)
            {
                _assistedConfirmCancel.clicked -= OnAssistedConfirmCancel;
                _assistedConfirmCancel.clicked += OnAssistedConfirmCancel;
            }

            if (panelStyles != null && !_root.styleSheets.Contains(panelStyles))
            {
                _root.styleSheets.Add(panelStyles);
            }

            HideAssistedConfirm();
            _wired = _intentLine != null && _blockReasonLine != null;
        }

        private void UnwireElements()
        {
            if (_groupDropdown != null)
            {
                _groupDropdown.UnregisterValueChangedCallback(OnGroupDropdownChanged);
            }

            if (_holdButton != null)
            {
                _holdButton.clicked -= OnHoldClicked;
            }

            if (_withdrawButton != null)
            {
                _withdrawButton.clicked -= OnWithdrawClicked;
            }

            if (_reattackButton != null)
            {
                _reattackButton.clicked -= OnReattackClicked;
            }

            if (_assistedConfirmAccept != null)
            {
                _assistedConfirmAccept.clicked -= OnAssistedConfirmAccept;
            }

            if (_assistedConfirmCancel != null)
            {
                _assistedConfirmCancel.clicked -= OnAssistedConfirmCancel;
            }
        }

        private void OnGroupDropdownChanged(ChangeEvent<string> evt)
        {
            if (!string.Equals(_selectedGroupId, evt.newValue, StringComparison.Ordinal))
            {
                SelectGroup(evt.newValue);
            }
        }

        private void ApplyPresentation(CoordinationPresentationState state)
        {
            if (_advisoryBadge != null)
            {
                _advisoryBadge.text = state.AdvisoryBadge;
            }

            SyncGroupDropdown(state.Groups);

            var group = state.FindGroup(_selectedGroupId);
            if (group == null)
            {
                if (_headerTitle != null) _headerTitle.text = "TASK GROUP COORDINATION";
                if (_intentLine != null) _intentLine.text = "No task groups registered.";
                if (_controllerLine != null) _controllerLine.text = "Controller: —";
                if (_blockReasonLine != null)
                {
                    _blockReasonLine.text = CoordinationBlockExplainer.Explain(CoordinationCommandBridge.ReasonUnknownGroup);
                    ApplyStatusClass(_blockReasonLine, "coord-status--blocked");
                }

                ApplyRows(_boundariesList, _boundaryRows, Array.Empty<string>(), null);
                ApplyRows(_effectsList, _effectRows, Array.Empty<string>(), null);
                _holdButton?.SetEnabled(false);
                _withdrawButton?.SetEnabled(false);
                _reattackButton?.SetEnabled(false);
                HideAssistedConfirm();
                return;
            }

            _selectedGroupId = group.GroupId;
            if (_groupDropdown != null && !string.Equals(_groupDropdown.value, group.GroupId, StringComparison.Ordinal))
            {
                _groupDropdown.SetValueWithoutNotify(group.GroupId);
            }

            if (_headerTitle != null) _headerTitle.text = "TASK GROUP COORDINATION · " + group.GroupId;
            if (_intentLine != null) _intentLine.text = group.IntentHeaderLine;
            if (_controllerLine != null) _controllerLine.text = group.ControllerLine;

            ApplyBoundaryRows(group.Boundaries);

            var holdPreview = group.GetPreview(CoordinationDecision.Hold);
            var withdrawPreview = group.GetPreview(CoordinationDecision.Withdraw);
            var reattackPreview = group.GetPreview(CoordinationDecision.Reattack);
            ApplyEffectRows(holdPreview, withdrawPreview, reattackPreview, group.GapLines);

            if (_blockReasonLine != null)
            {
                _blockReasonLine.text = group.ActiveBlockExplanation;
                var css = string.IsNullOrEmpty(group.ActiveBlockReasonCode)
                    ? (group.GapLines.Count > 0 ? "coord-status--degraded" : "coord-status--ok")
                    : "coord-status--blocked";
                ApplyStatusClass(_blockReasonLine, css);
            }

            _holdButton?.SetEnabled(holdPreview.IsAllowed);
            _withdrawButton?.SetEnabled(withdrawPreview.IsAllowed);
            _reattackButton?.SetEnabled(true);
        }

        private void SyncGroupDropdown(IReadOnlyList<CoordinationGroupPresentation> groups)
        {
            if (_groupDropdown == null)
            {
                return;
            }

            var changed = _groupChoices.Count != groups.Count;
            if (!changed)
            {
                for (var i = 0; i < groups.Count; i++)
                {
                    if (!string.Equals(_groupChoices[i], groups[i].GroupId, StringComparison.Ordinal))
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (!changed)
            {
                return;
            }

            _groupChoices.Clear();
            for (var i = 0; i < groups.Count; i++)
            {
                _groupChoices.Add(groups[i].GroupId);
            }

            _groupDropdown.choices = _groupChoices;
            if (_groupChoices.Count > 0 && string.IsNullOrEmpty(_selectedGroupId))
            {
                _selectedGroupId = _groupChoices[0];
            }
        }

        private void ApplyBoundaryRows(IReadOnlyList<CoordinationResponsibilityBoundary> boundaries)
        {
            if (_boundariesList == null)
            {
                return;
            }

            EnsureRowPool(_boundariesList, _boundaryRows, boundaries.Count);
            for (var i = 0; i < _boundaryRows.Count; i++)
            {
                var row = _boundaryRows[i];
                if (i >= boundaries.Count)
                {
                    row.style.display = DisplayStyle.None;
                    continue;
                }

                var b = boundaries[i];
                row.text = "[" + b.Kind + " · " + b.StatusLabel + "] " + b.Detail;
                var css = b.StatusLabel == "ACTIVE"
                    ? "coord-status--ok"
                    : b.StatusLabel == "GAP"
                        ? "coord-status--blocked"
                        : "coord-status--degraded";
                ApplyStatusClass(row, css);
                row.style.display = DisplayStyle.Flex;
            }
        }

        private void ApplyEffectRows(
            CoordinationCommandEffectPreview holdPreview,
            CoordinationCommandEffectPreview withdrawPreview,
            CoordinationCommandEffectPreview reattackPreview,
            IReadOnlyList<string> gapLines)
        {
            if (_effectsList == null)
            {
                return;
            }

            var unitCount = holdPreview.UnitEffects.Count;
            var totalRows = 3 + unitCount + gapLines.Count;
            EnsureRowPool(_effectsList, _effectRows, totalRows);

            var idx = 0;
            SetEffectRow(idx++, holdPreview.SummaryLine, holdPreview.IsAllowed ? "coord-status--ok" : "coord-status--blocked");
            SetEffectRow(idx++, withdrawPreview.SummaryLine, withdrawPreview.IsAllowed ? "coord-status--ok" : "coord-status--blocked");
            SetEffectRow(idx++, reattackPreview.SummaryLine, "coord-status--advisory");

            for (var i = 0; i < unitCount; i++)
            {
                var unit = holdPreview.UnitEffects[i];
                var css = unit.EffectStatus == "WILL_APPLY"
                    ? "coord-status--ok"
                    : unit.EffectStatus == "EXCLUDED_DETACHED"
                        ? "coord-status--degraded"
                        : "coord-status--blocked";
                SetEffectRow(idx++, unit.EffectSummary, css);
            }

            for (var i = 0; i < gapLines.Count; i++)
            {
                SetEffectRow(idx++, gapLines[i], "coord-status--degraded");
            }

            for (var i = idx; i < _effectRows.Count; i++)
            {
                _effectRows[i].style.display = DisplayStyle.None;
            }
        }

        private void SetEffectRow(int index, string text, string cssClass)
        {
            var row = _effectRows[index];
            row.text = text;
            ApplyStatusClass(row, cssClass);
            row.style.display = DisplayStyle.Flex;
        }

        private static void ApplyRows(
            VisualElement? container,
            List<Label> pool,
            IReadOnlyList<string> lines,
            string? cssClass)
        {
            if (container == null)
            {
                return;
            }

            EnsureRowPool(container, pool, lines.Count);
            for (var i = 0; i < pool.Count; i++)
            {
                var row = pool[i];
                if (i >= lines.Count)
                {
                    row.style.display = DisplayStyle.None;
                    continue;
                }

                row.text = lines[i];
                if (cssClass != null)
                {
                    ApplyStatusClass(row, cssClass);
                }

                row.style.display = DisplayStyle.Flex;
            }
        }

        private static void EnsureRowPool(VisualElement container, List<Label> pool, int required)
        {
            while (pool.Count < required)
            {
                var row = new Label();
                row.AddToClassList("coord-row");
                container.Add(row);
                pool.Add(row);
            }
        }

        private static void ApplyStatusClass(VisualElement element, string activeClass)
        {
            for (var i = 0; i < StatusClasses.Length; i++)
            {
                element.RemoveFromClassList(StatusClasses[i]);
            }

            element.AddToClassList(activeClass);
        }

        private void OnHoldClicked() => SubmitOrStageDecision(CoordinationDecision.Hold);

        private void OnWithdrawClicked() => SubmitOrStageDecision(CoordinationDecision.Withdraw);

        private void OnReattackClicked()
        {
            var explanation = CoordinationBlockExplainer.Explain(CoordinationCommandBridge.ReasonRetattackAdvisoryOnly);
            SetFeedback(explanation, CoordinationCommandBridge.ReasonRetattackAdvisoryOnly);
        }

        private void SubmitOrStageDecision(CoordinationDecision decision)
        {
            if (bridgeHost == null)
            {
                return;
            }

            var group = _lastPresentation.FindGroup(_selectedGroupId);
            if (group == null)
            {
                SetFeedback(
                    CoordinationBlockExplainer.Explain(CoordinationCommandBridge.ReasonUnknownGroup),
                    CoordinationCommandBridge.ReasonUnknownGroup);
                return;
            }

            if (decision == CoordinationDecision.Withdraw && !bridgeHost.CommandTimeline.IsInspecting)
            {
                var autonomy = ResolveSnapshotAutonomy(group.GroupId);
                if (CoordinationAssistedWithdrawRedeployConfirmBinder.RequiresExplicitConfirm(autonomy, decision))
                {
                    var coordSnapshot = FindCoordinationGroup(group.GroupId);
                    var input = new CoordinationAssistedWithdrawRedeployConfirmInput(
                        autonomy,
                        group.GroupId,
                        decision,
                        coordSnapshot?.Intent);
                    if (CoordinationAssistedWithdrawRedeployConfirmBinder.TryBegin(input, out var chrome))
                    {
                        ShowAssistedConfirm(chrome);
                        SetFeedback("Assisted withdraw confirmation required.", null);
                        return;
                    }
                }
            }

            ExecuteSubmit(group.GroupId, decision);
        }

        private void OnAssistedConfirmAccept()
        {
            if (bridgeHost == null)
            {
                return;
            }

            var result = CoordinationAssistedWithdrawRedeployConfirmBinder.Confirm(
                (gid, dec) => bridgeHost.SubmitGroupDecision(gid, dec));
            HideAssistedConfirm();
            CoordinationBlockExplainer.TryExtractReasonCode(result.StatusLine, out var code);
            SetFeedback(result.StatusLine, string.IsNullOrEmpty(code) ? null : code);
            ForceRefresh();
        }

        private void OnAssistedConfirmCancel()
        {
            CoordinationAssistedWithdrawRedeployConfirmBinder.TryCancel();
            HideAssistedConfirm();
            SetFeedback("Cancelled assisted withdraw confirmation; order log unchanged.", null);
        }

        private void ExecuteSubmit(string groupId, CoordinationDecision decision)
        {
            if (bridgeHost == null)
            {
                return;
            }

            CoordinationAssistedWithdrawRedeployConfirmBinder.TryCancel();
            HideAssistedConfirm();

            var feedback = bridgeHost.SubmitGroupDecision(groupId, decision);
            CoordinationBlockExplainer.TryExtractReasonCode(feedback, out var code);
            SetFeedback(feedback, string.IsNullOrEmpty(code) ? null : code);
            ForceRefresh();
        }

        private void ForceRefresh()
        {
            _lastCoordination = null;
            _lastFingerprint = null;
            Refresh();
        }

        private void SetFeedback(string message, string? reasonCode)
        {
            _lastFeedback = message;
            if (_feedbackLine != null)
            {
                _feedbackLine.text = string.IsNullOrEmpty(reasonCode)
                    ? message
                    : message + " — " + CoordinationBlockExplainer.Explain(reasonCode);
            }
        }

        private void ShowAssistedConfirm(CoordinationAssistedWithdrawRedeployChrome chrome)
        {
            if (_assistedConfirmBox == null)
            {
                return;
            }

            var labels = CoordinationAssistedWithdrawRedeployPanelBinder.Bind(chrome);
            if (_assistedConfirmTitle != null) _assistedConfirmTitle.text = labels.Title;
            if (_assistedConfirmBody != null) _assistedConfirmBody.text = labels.Body;
            if (_assistedConfirmAccept != null) _assistedConfirmAccept.text = labels.ConfirmLabel;
            if (_assistedConfirmCancel != null) _assistedConfirmCancel.text = labels.CancelLabel;
            _assistedConfirmBox.style.display = DisplayStyle.Flex;
        }

        private void HideAssistedConfirm()
        {
            if (_assistedConfirmBox != null)
            {
                _assistedConfirmBox.style.display = DisplayStyle.None;
            }
        }

        private AutonomyLevel ResolveSnapshotAutonomy(string groupId)
        {
            var group = FindCoordinationGroup(groupId);
            return group?.Details?.DelegationAutonomy ?? AutonomyLevel.Manual;
        }

        private CoordinationGroupSnapshot? FindCoordinationGroup(string groupId)
        {
            if (bridgeHost == null)
            {
                return null;
            }

            var groups = bridgeHost.LastCoordination.Groups;
            for (var i = 0; i < groups.Count; i++)
            {
                if (string.Equals(groups[i].Coordination.GroupId, groupId, StringComparison.Ordinal))
                {
                    return groups[i];
                }
            }

            return null;
        }
    }
}
#endif
