// S29-04: ADR-011 Phase E platform workbook import → propose → approve via PlatformWorkbookWriteBridge.
// Staging review UX: entity-level diff preview; approve disabled until review acknowledged.
#if UNITY_5_3_OR_NEWER
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

        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;
        [SerializeField] private string databasePathForImport = string.Empty;
        [SerializeField] private string actorType = "unity";
        [SerializeField] private string actorId = "platform-import-host";
        [SerializeField] private string reviewerActorType = "human";
        [SerializeField] private string reviewerActorId = "curator";

        private UIDocument _document = null!;
        private TextField? _workbookPathField;
        private Button? _proposeButton;
        private Button? _approveButton;
        private Button? _rejectButton;
        private ListView? _diffList;
        private Label? _statusLine;
        private Toggle? _acknowledgeToggle;
        private string? _boundDatabasePath;
        private PlatformWorkbook? _boundWorkbook;
        private PlatformWorkbookWriteResult? _lastProposeResult;
        private bool _reviewAcknowledged;
        private List<string> _diffDisplayItems = new();
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
            ApplyPanelState(PlatformImportStagingProjection.Bind(_lastProposeResult, _reviewAcknowledged));
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

        private void TryWireElements()
        {
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            if (_workbookPathField == null)
            {
                _workbookPathField = root.Q<TextField>(WorkbookPathName);
            }

            if (_diffList == null)
            {
                _diffList = root.Q<ListView>(DiffListName);
                if (_diffList != null)
                {
                    _diffList.makeItem = () => new Label();
                    _diffList.bindItem = (element, index) =>
                    {
                        if (element is Label label && index >= 0 && index < _diffDisplayItems.Count)
                        {
                            label.text = _diffDisplayItems[index];
                        }
                    };
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

            if (_proposeButton == null)
            {
                _proposeButton = root.Q<Button>(ProposeButtonName);
                if (_proposeButton != null)
                {
                    _proposeButton.clicked -= OnProposeClicked;
                    _proposeButton.clicked += OnProposeClicked;
                }
            }

            if (_approveButton == null)
            {
                _approveButton = root.Q<Button>(ApproveButtonName);
                if (_approveButton != null)
                {
                    _approveButton.clicked -= OnApproveClicked;
                    _approveButton.clicked += OnApproveClicked;
                }
            }

            if (_rejectButton == null)
            {
                _rejectButton = root.Q<Button>(RejectButtonName);
                if (_rejectButton != null)
                {
                    _rejectButton.clicked -= OnRejectClicked;
                    _rejectButton.clicked += OnRejectClicked;
                }
            }

            _wired = _diffList != null && _proposeButton != null;
        }

        private string? ResolveDatabasePath()
        {
            if (!string.IsNullOrWhiteSpace(_boundDatabasePath))
            {
                return _boundDatabasePath;
            }

            return string.IsNullOrWhiteSpace(databasePathForImport) ? null : databasePathForImport;
        }

        private void OnProposeClicked()
        {
            var databasePath = ResolveDatabasePath();
            if (string.IsNullOrWhiteSpace(databasePath))
            {
                Debug.LogWarning("[PlatformImportPanelHost] Propose skipped: no database path bound.");
                ApplyPanelState(PlatformImportStagingProjection.Bind(null, _reviewAcknowledged) with
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
                        ApplyPanelState(PlatformImportStagingProjection.Bind(null, _reviewAcknowledged) with
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

                ApplyPanelState(PlatformImportStagingProjection.Bind(_lastProposeResult, _reviewAcknowledged));
                Debug.Log(
                    $"[PlatformImportPanelHost] Propose ok proposed={_lastProposeResult.Proposed} " +
                    $"batchCount={_lastProposeResult.BatchIds.Count} changes={_lastProposeResult.Import.Plan.Changes.Count}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlatformImportPanelHost] Propose failed: {ex.Message}");
                ApplyPanelState(PlatformImportStagingProjection.Bind(null, _reviewAcknowledged) with
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

            var panelState = PlatformImportStagingProjection.Bind(_lastProposeResult, _reviewAcknowledged);
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
                ApplyPanelState(PlatformImportStagingProjection.Bind(null, _reviewAcknowledged) with
                {
                    StatusLine = approve.AllCommitted
                        ? $"STAGING: approved {approve.CommittedBatchIds.Count} batch(es)"
                        : "STAGING: approve completed with errors — see logs",
                });
                Debug.Log(
                    $"[PlatformImportPanelHost] Approve ok committed={approve.CommittedBatchIds.Count} " +
                    $"processed={approve.ProcessedBatchIds.Count}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlatformImportPanelHost] Approve failed: {ex.Message}");
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

            var panelState = PlatformImportStagingProjection.Bind(_lastProposeResult, _reviewAcknowledged);
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
                ApplyPanelState(PlatformImportStagingProjection.Bind(null, _reviewAcknowledged) with
                {
                    StatusLine = reject.AllCommitted
                        ? "STAGING: reject completed unexpectedly — batches committed"
                        : $"STAGING: rejected {reject.ProcessedBatchIds.Count} batch(es)",
                });
                Debug.Log($"[PlatformImportPanelHost] Reject ok processed={reject.ProcessedBatchIds.Count}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlatformImportPanelHost] Reject failed: {ex.Message}");
            }
        }

        private void OnAcknowledgeChanged(ChangeEvent<bool> evt)
        {
            _reviewAcknowledged = evt.newValue;
            ApplyPanelState(PlatformImportStagingProjection.Bind(_lastProposeResult, _reviewAcknowledged));
        }

        private void ApplyPanelState(PlatformImportStagingPanelState panelState)
        {
            _diffDisplayItems = panelState.DiffRows.Select(row => row.SummaryLine).ToList();

            if (_diffList != null)
            {
                _diffList.itemsSource = _diffDisplayItems;
                _diffList.Rebuild();
            }

            if (_statusLine != null)
            {
                _statusLine.text = panelState.StatusLine;
            }

            if (_approveButton != null)
            {
                _approveButton.SetEnabled(panelState.ApproveEnabled);
            }

            if (_rejectButton != null)
            {
                _rejectButton.SetEnabled(panelState.RejectEnabled);
            }
        }

        private void ClearStagingState()
        {
            _lastProposeResult = null;
            _reviewAcknowledged = false;
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