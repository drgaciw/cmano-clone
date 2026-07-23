// S29-04: ADR-011 Phase E platform workbook import → propose → approve via PlatformWorkbookWriteBridge.
// Staging review UX: entity-level diff preview; approve disabled until review acknowledged.
// PE-UX-W1: section filters, colored DiffKind rows, blocked status class (P-PE-04).
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using System.Linq;
using ProjectAegis.Data.Platform;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class PlatformImportPanelHost : MonoBehaviour
    {
        private const string RootName = "platform-import-root";
        private const string WorkbookPathName = "platform-import-workbook-path";
        private const string ProposeButtonName = "platform-import-propose";
        private const string DiffListName = "platform-import-diff-list";
        private const string StatusName = "platform-import-status";
        private const string AcknowledgeName = "platform-import-acknowledge";
        private const string ApproveButtonName = "platform-import-approve";
        private const string RejectButtonName = "platform-import-reject";
        private const string FilterAllName = "platform-import-filter-all";
        private const string FilterDamageName = "platform-import-filter-damage";
        private const string FilterCommsName = "platform-import-filter-comms";
        private const string FilterLinkName = "platform-import-filter-link";
        private const string FilterOtherName = "platform-import-filter-other";
        private const string SectionFilterActiveClass = "platform-import-section-filter--active";
        private const string StatusBlockedClass = "platform-import-status--blocked";
        private const string DiffRowBaseClass = "platform-import-diff-row";

        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;
        [SerializeField] private string databasePathForImport = string.Empty;
        [SerializeField] private string actorType = "unity";
        [SerializeField] private string actorId = "platform-import-host";
        [SerializeField] private string reviewerActorType = "human";
        [SerializeField] private string reviewerActorId = "curator";
        [SerializeField] private C2AccessibilityScalePercent accessibilityScale = C2AccessibilityScalePercent.OneHundred;
        [SerializeField] private bool reducedMotion = true;

        private UIDocument _document = null!;
        private TextField? _workbookPathField;
        private Button? _proposeButton;
        private Button? _approveButton;
        private Button? _rejectButton;
        private Button? _filterAllButton;
        private Button? _filterDamageButton;
        private Button? _filterCommsButton;
        private Button? _filterLinkButton;
        private Button? _filterOtherButton;
        private VisualElement? _sectionFilters;
        private ListView? _diffList;
        private Label? _statusLine;
        private Toggle? _acknowledgeToggle;
        private string? _boundDatabasePath;
        private PlatformWorkbook? _boundWorkbook;
        private PlatformWorkbookWriteResult? _lastProposeResult;
        private bool _reviewAcknowledged;
        private PlatformImportStagingSection? _sectionFilter;
        private List<PlatformImportStagingRow> _diffRows = new();
        private int _blockedFindingCount;
        private long _clockTicks;
        private bool _wired;

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
            RefreshPanelState();
            ApplyPanelVisibility();
        }

        public void BindDatabasePath(string databasePath)
        {
            _boundDatabasePath = databasePath;
            TryWireElements();
        }

        public void BindWorkbook(PlatformWorkbook workbook)
        {
            _boundWorkbook = workbook;
            TryWireElements();
        }

        public PlatformWorkbookWriteResult? LastProposeResult => _lastProposeResult;

        public bool ReviewAcknowledged => _reviewAcknowledged;

        /// <summary>PE-UX-W5: pending staging row count for shell health strip.</summary>
        public int PendingDiffCount => _diffRows.Count;

        /// <summary>PE-UX-W5: blocked finding count from last propose bind.</summary>
        public int BlockedFindingCount => _blockedFindingCount;

        /// <summary>PE-UX-W4: shell toggles visibility without disabling host (staging preserved).</summary>
        public void SetShellVisible(bool visible)
        {
            showPanel = visible;
            ApplyPanelVisibility();
        }

        /// <summary>PE-UX-W5: set section filter from shell / curator jump (e.g. LINK).</summary>
        public void SetSectionFilter(PlatformImportStagingSection? section)
        {
            _sectionFilter = section;
            RefreshPanelState();
        }

        private void TryWireElements()
        {
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            ApplyAccessibility(root);

            if (_workbookPathField == null)
            {
                _workbookPathField = root.Q<TextField>(WorkbookPathName);
            }

            if (_diffList == null)
            {
                _diffList = root.Q<ListView>(DiffListName);
                if (_diffList != null)
                {
                    _diffList.makeItem = () =>
                    {
                        var label = new Label();
                        label.AddToClassList(DiffRowBaseClass);
                        return label;
                    };
                    _diffList.bindItem = (element, index) =>
                    {
                        if (element is not Label label || index < 0 || index >= _diffRows.Count)
                        {
                            return;
                        }

                        var row = _diffRows[index];
                        label.text = PlatformImportStagingProjection.FormatDisplayLine(row);
                        label.ClearClassList();
                        label.AddToClassList(DiffRowBaseClass);
                        label.AddToClassList(row.UssClass);
                        label.tooltip = label.text;
                    };
                }
            }

            if (_sectionFilters == null)
            {
                _sectionFilters = root.Q("platform-import-section-filters");
                if (_sectionFilters != null)
                {
                    _sectionFilters.focusable = true;
                    _sectionFilters.RegisterCallback<KeyDownEvent>(OnSectionFiltersKeyDown);
                }
            }

            if (_statusLine == null)
            {
                _statusLine = root.Q<Label>(StatusName);
            }

            if (_acknowledgeToggle == null)
            {
                _acknowledgeToggle = root.Q<Toggle>(AcknowledgeName);
                if (_acknowledgeToggle != null)
                {
                    _acknowledgeToggle.RegisterValueChangedCallback(OnAcknowledgeChanged);
                }
            }

            WireActionButton(ref _proposeButton, ProposeButtonName, OnProposeClicked);
            WireActionButton(ref _approveButton, ApproveButtonName, OnApproveClicked);
            WireActionButton(ref _rejectButton, RejectButtonName, OnRejectClicked);
            WireFilterButton(ref _filterAllButton, FilterAllName, OnFilterAllClicked);
            WireFilterButton(ref _filterDamageButton, FilterDamageName, OnFilterDamageClicked);
            WireFilterButton(ref _filterCommsButton, FilterCommsName, OnFilterCommsClicked);
            WireFilterButton(ref _filterLinkButton, FilterLinkName, OnFilterLinkClicked);
            WireFilterButton(ref _filterOtherButton, FilterOtherName, OnFilterOtherClicked);

            _wired = _diffList != null && _proposeButton != null;
        }

        private void WireActionButton(ref Button? field, string name, System.Action handler)
        {
            if (field != null)
            {
                return;
            }

            var root = _document.rootVisualElement;
            field = root?.Q<Button>(name);
            if (field != null)
            {
                field.clicked -= handler;
                field.clicked += handler;
            }
        }

        private void WireFilterButton(ref Button? field, string name, System.Action handler)
        {
            if (field != null)
            {
                return;
            }

            var root = _document.rootVisualElement;
            field = root?.Q<Button>(name);
            if (field != null)
            {
                field.clicked -= handler;
                field.clicked += handler;
            }
        }

        private void OnFilterAllClicked() => OnSectionFilterClicked(null);

        private void OnFilterDamageClicked() => OnSectionFilterClicked(PlatformImportStagingSection.Damage);

        private void OnFilterCommsClicked() => OnSectionFilterClicked(PlatformImportStagingSection.Comms);

        private void OnFilterLinkClicked() => OnSectionFilterClicked(PlatformImportStagingSection.Link);

        private void OnFilterOtherClicked() => OnSectionFilterClicked(PlatformImportStagingSection.Other);

        private string? ResolveDatabasePath()
        {
            if (!string.IsNullOrWhiteSpace(_boundDatabasePath))
            {
                return _boundDatabasePath;
            }

            return string.IsNullOrWhiteSpace(databasePathForImport) ? null : databasePathForImport;
        }

        private void OnSectionFilterClicked(PlatformImportStagingSection? section)
        {
            _sectionFilter = section;
            RefreshPanelState();
        }

        private void OnProposeClicked()
        {
            var databasePath = ResolveDatabasePath();
            if (string.IsNullOrWhiteSpace(databasePath))
            {
                Debug.LogWarning("[PlatformImportPanelHost] Propose skipped: no database path bound.");
                ApplyPanelState(PlatformImportStagingProjection.Bind(null, _reviewAcknowledged, _sectionFilter) with
                {
                    StatusLine = "STAGING: database path required before propose",
                });
                return;
            }

            try
            {
                _clockTicks++;
                if (_boundWorkbook != null)
                {
                    _lastProposeResult = PlatformWorkbookWriteBridge.ProposeWorkbook(
                        databasePath,
                        _boundWorkbook,
                        actorType,
                        actorId,
                        clockTicks: _clockTicks,
                        rationale: "unity import panel propose");
                }
                else
                {
                    var workbookPath = _workbookPathField?.value?.Trim();
                    if (string.IsNullOrWhiteSpace(workbookPath))
                    {
                        ApplyPanelState(PlatformImportStagingProjection.Bind(null, _reviewAcknowledged, _sectionFilter) with
                        {
                            StatusLine = "STAGING: workbook path or bound workbook required",
                        });
                        return;
                    }

                    _lastProposeResult = PlatformWorkbookWriteBridge.ProposeWorkbookFromFile(
                        databasePath,
                        workbookPath,
                        actorType,
                        actorId,
                        clockTicks: _clockTicks,
                        rationale: "unity import panel propose from file");
                }

                _reviewAcknowledged = false;
                if (_acknowledgeToggle != null)
                {
                    _acknowledgeToggle.SetValueWithoutNotify(false);
                }

                RefreshPanelState();
                Debug.Log(
                    $"[PlatformImportPanelHost] Propose ok proposed={_lastProposeResult.Proposed} " +
                    $"batchCount={_lastProposeResult.BatchIds.Count} changes={_lastProposeResult.Import.Plan.Changes.Count}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[PlatformImportPanelHost] Propose failed: {ex.Message}");
                ApplyPanelState(PlatformImportStagingProjection.Bind(null, _reviewAcknowledged, _sectionFilter) with
                {
                    StatusLine = $"STAGING: propose failed — {ex.Message}",
                });
            }
        }

        private void OnApproveClicked()
        {
            var databasePath = ResolveDatabasePath();
            if (string.IsNullOrWhiteSpace(databasePath) || _lastProposeResult is null)
            {
                Debug.LogWarning("[PlatformImportPanelHost] Approve skipped: no staged batches.");
                return;
            }

            var panelState = PlatformImportStagingProjection.Bind(_lastProposeResult, _reviewAcknowledged, _sectionFilter);
            if (!panelState.ApproveEnabled)
            {
                Debug.LogWarning("[PlatformImportPanelHost] Approve skipped: review not acknowledged or no pending batches.");
                return;
            }

            try
            {
                _clockTicks++;
                var approve = PlatformWorkbookWriteBridge.ApproveBatches(
                    databasePath,
                    _lastProposeResult.BatchIds,
                    reviewerActorType,
                    reviewerActorId,
                    clockTicks: _clockTicks);
                ClearStagingState();
                ApplyPanelState(PlatformImportStagingProjection.Bind(null, _reviewAcknowledged, _sectionFilter) with
                {
                    StatusLine = approve.AllCommitted
                        ? $"STAGING: approved {approve.CommittedBatchIds.Count} batch(es)"
                        : "STAGING: approve completed with errors — see logs",
                });
                Debug.Log(
                    $"[PlatformImportPanelHost] Approve ok committed={approve.CommittedBatchIds.Count} " +
                    $"processed={approve.ProcessedBatchIds.Count}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[PlatformImportPanelHost] Approve failed: {ex.Message}");
                ApplyPanelState(PlatformImportStagingProjection.Bind(_lastProposeResult, _reviewAcknowledged, _sectionFilter) with
                {
                    StatusLine = $"STAGING: approve failed — {ex.Message}",
                });
            }
        }

        private void OnRejectClicked()
        {
            var databasePath = ResolveDatabasePath();
            if (string.IsNullOrWhiteSpace(databasePath) || _lastProposeResult is null)
            {
                Debug.LogWarning("[PlatformImportPanelHost] Reject skipped: no staged batches.");
                return;
            }

            var panelState = PlatformImportStagingProjection.Bind(_lastProposeResult, _reviewAcknowledged, _sectionFilter);
            if (!panelState.RejectEnabled)
            {
                Debug.LogWarning("[PlatformImportPanelHost] Reject skipped: no pending batches.");
                return;
            }

            try
            {
                _clockTicks++;
                var reject = PlatformWorkbookWriteBridge.RejectBatches(
                    databasePath,
                    _lastProposeResult.BatchIds,
                    reviewerActorType,
                    reviewerActorId,
                    clockTicks: _clockTicks,
                    rationale: "unity import panel reject");
                ClearStagingState();
                ApplyPanelState(PlatformImportStagingProjection.Bind(null, _reviewAcknowledged, _sectionFilter) with
                {
                    StatusLine = reject.AllCommitted
                        ? "STAGING: reject completed unexpectedly — batches committed"
                        : $"STAGING: rejected {reject.ProcessedBatchIds.Count} batch(es)",
                });
                Debug.Log($"[PlatformImportPanelHost] Reject ok processed={reject.ProcessedBatchIds.Count}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[PlatformImportPanelHost] Reject failed: {ex.Message}");
                ApplyPanelState(PlatformImportStagingProjection.Bind(_lastProposeResult, _reviewAcknowledged, _sectionFilter) with
                {
                    StatusLine = $"STAGING: reject failed — {ex.Message}",
                });
            }
        }

        private void OnAcknowledgeChanged(ChangeEvent<bool> evt)
        {
            _reviewAcknowledged = evt.newValue;
            RefreshPanelState();
        }

        private void RefreshPanelState()
        {
            ApplyPanelState(PlatformImportStagingProjection.Bind(_lastProposeResult, _reviewAcknowledged, _sectionFilter));
        }

        private void ApplyPanelState(PlatformImportStagingPanelState panelState)
        {
            _diffRows = panelState.DiffRows.ToList();
            _blockedFindingCount = panelState.IsBlocked
                ? _diffRows.Count(r => r.DiffKind == PlatformImportStagingDiffKind.Blocked)
                : 0;
            if (panelState.IsBlocked && _blockedFindingCount == 0)
            {
                _blockedFindingCount = 1;
            }

            if (_diffList != null)
            {
                _diffList.itemsSource = _diffRows;
                _diffList.Rebuild();
            }

            if (_statusLine != null)
            {
                _statusLine.text = panelState.StatusLine;
                _statusLine.EnableInClassList(StatusBlockedClass, panelState.IsBlocked);
            }

            if (_approveButton != null)
            {
                _approveButton.SetEnabled(panelState.ApproveEnabled);
            }

            if (_rejectButton != null)
            {
                _rejectButton.SetEnabled(panelState.RejectEnabled);
            }

            ApplySectionFilterChrome();
        }

        private void OnSectionFiltersKeyDown(KeyDownEvent evt)
        {
            PlatformImportStagingSection?[] order =
            [
                null,
                PlatformImportStagingSection.Damage,
                PlatformImportStagingSection.Comms,
                PlatformImportStagingSection.Link,
                PlatformImportStagingSection.Other,
            ];
            var index = 0;
            for (var i = 0; i < order.Length; i++)
            {
                if (order[i] == _sectionFilter)
                {
                    index = i;
                    break;
                }
            }

            if (evt.keyCode == KeyCode.RightArrow)
            {
                index = (index + 1) % order.Length;
                OnSectionFilterClicked(order[index]);
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.LeftArrow)
            {
                index = (index - 1 + order.Length) % order.Length;
                OnSectionFilterClicked(order[index]);
                evt.StopPropagation();
            }
        }

        private void ApplyAccessibility(VisualElement root)
        {
            var settings = new C2AccessibilitySettings(accessibilityScale, reducedMotion);
            root.EnableInClassList("aegis-scale-100", settings.ScalePercent == C2AccessibilityScalePercent.OneHundred);
            root.EnableInClassList("aegis-scale-125", settings.ScalePercent == C2AccessibilityScalePercent.OneTwentyFive);
            root.EnableInClassList("aegis-scale-150", settings.ScalePercent == C2AccessibilityScalePercent.OneFifty);
            root.EnableInClassList("reduced-motion", settings.ReducedMotion);
        }

        private void ApplySectionFilterChrome()
        {
            SetFilterActive(_filterAllButton, _sectionFilter is null);
            SetFilterActive(_filterDamageButton, _sectionFilter == PlatformImportStagingSection.Damage);
            SetFilterActive(_filterCommsButton, _sectionFilter == PlatformImportStagingSection.Comms);
            SetFilterActive(_filterLinkButton, _sectionFilter == PlatformImportStagingSection.Link);
            SetFilterActive(_filterOtherButton, _sectionFilter == PlatformImportStagingSection.Other);
        }

        private static void SetFilterActive(Button? button, bool active)
        {
            button?.EnableInClassList(SectionFilterActiveClass, active);
        }

        private void ClearStagingState()
        {
            _lastProposeResult = null;
            _reviewAcknowledged = false;
            _blockedFindingCount = 0;
            if (_acknowledgeToggle != null)
            {
                _acknowledgeToggle.SetValueWithoutNotify(false);
            }
        }

        private void ApplyPanelVisibility()
        {
            var root = _document.rootVisualElement?.Q(RootName);
            if (root != null)
            {
                root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
#endif
