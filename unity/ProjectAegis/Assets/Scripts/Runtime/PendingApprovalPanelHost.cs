// DRG-66: Pending Approval panel — APPROVE / REJECT actions for queued agent orders.
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class PendingApprovalPanelHost : MonoBehaviour
    {
        private const string RootName = "pending-approval-root";
        private const string HeaderName = "pending-approval-header";
        private const string BadgeName = "pending-approval-badge";
        private const string EmptyName = "pending-approval-empty";
        private const string ListName = "pending-approval-list";
        private const string ApproveButtonName = "pending-approval-approve";
        private const string RejectButtonName = "pending-approval-reject";
        private const string StatusName = "pending-approval-status";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _headerLine;
        private Label? _badgeLine;
        private Label? _emptyLine;
        private Label? _statusLine;
        private ListView? _approvalList;
        private Button? _approveButton;
        private Button? _rejectButton;
        private IReadOnlyList<PendingApprovalRow> _rows = System.Array.Empty<PendingApprovalRow>();
        private bool _wired;
        private bool _handlersRegistered;
        private string? _lastStatus;

        /// <summary>Last status line set by APPROVE or REJECT action.</summary>
        public string? LastStatus => _lastStatus;

        /// <summary>Currently projected approval rows (read-only; refreshed each LateUpdate).</summary>
        public IReadOnlyList<PendingApprovalRow> Rows => _rows;

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
            _headerLine = panel.Q<Label>(HeaderName);
            _badgeLine = panel.Q<Label>(BadgeName);
            _emptyLine = panel.Q<Label>(EmptyName);
            _statusLine = panel.Q<Label>(StatusName);
            _approvalList = panel.Q<ListView>(ListName);
            _approveButton = panel.Q<Button>(ApproveButtonName);
            _rejectButton = panel.Q<Button>(RejectButtonName);

            if (_approvalList != null)
            {
                _approvalList.makeItem = () => new Label();
                _approvalList.bindItem = (element, index) =>
                {
                    if (element is Label label && index >= 0 && index < _rows.Count)
                    {
                        label.text = $"{_rows[index].SummaryLine}  {_rows[index].RiskLabel}";
                    }
                };
                _approvalList.selectionType = SelectionType.Single;
                _approvalList.selectionChanged -= OnSelectionChanged;
                _approvalList.selectionChanged += OnSelectionChanged;
            }

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }

            WireClickHandlers();
            _wired = _approvalList != null || _approveButton != null || _rejectButton != null;
        }

        private void WireClickHandlers()
        {
            if (_handlersRegistered)
            {
                return;
            }

            if (_approveButton != null)
            {
                _approveButton.clicked += OnApproveClicked;
            }

            if (_rejectButton != null)
            {
                _rejectButton.clicked += OnRejectClicked;
            }

            _handlersRegistered = true;
        }

        private void OnSelectionChanged(IEnumerable<object> _)
        {
            UpdateActionButtons();
        }

        private void OnApproveClicked()
        {
            var row = SelectedRow();
            if (row == null)
            {
                SetStatus("NO_SELECTION");
                return;
            }

            if (bridgeHost?.Bridge?.Orchestrator == null)
            {
                SetStatus("NO_BRIDGE");
                return;
            }

            if (bridgeHost.Bridge.Orchestrator.TryApprovePendingOrder(row.OrderId))
            {
                SetStatus(null);
                Refresh();
            }
            else
            {
                SetStatus("APPROVE_FAILED");
            }
        }

        private void OnRejectClicked()
        {
            var row = SelectedRow();
            if (row == null)
            {
                SetStatus("NO_SELECTION");
                return;
            }

            if (bridgeHost?.Bridge?.Orchestrator == null)
            {
                SetStatus("NO_BRIDGE");
                return;
            }

            if (bridgeHost.Bridge.Orchestrator.TryRejectPendingOrder(row.OrderId))
            {
                SetStatus(null);
                Refresh();
            }
            else
            {
                SetStatus("REJECT_FAILED");
            }
        }

        private void Refresh()
        {
            if (!_wired || bridgeHost?.Bridge?.Orchestrator == null)
            {
                return;
            }

            var pending = bridgeHost.Bridge.Orchestrator.PendingApprovals;
            _rows = PendingApprovalProjection.Project(pending);

            if (_headerLine != null)
            {
                _headerLine.text = PendingApprovalProjection.HeaderLine;
            }

            if (_badgeLine != null)
            {
                _badgeLine.text = PendingApprovalProjection.FormatBadge(pending);
            }

            if (_emptyLine != null)
            {
                _emptyLine.text = _rows.Count == 0
                    ? PendingApprovalProjection.EmptyStateLine
                    : string.Empty;
                _emptyLine.style.display = _rows.Count == 0
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            if (_approvalList != null)
            {
                _approvalList.itemsSource = (System.Collections.IList)_rows;
                _approvalList.Rebuild();
            }

            UpdateActionButtons();

            var rootEl = _document.rootVisualElement?.Q(RootName);
            if (rootEl != null)
            {
                rootEl.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void UpdateActionButtons()
        {
            var hasSelection = SelectedRow() != null;
            if (_approveButton != null)
            {
                _approveButton.SetEnabled(hasSelection);
                _approveButton.tooltip = hasSelection ? "Approve selected order" : "Select an order first";
            }

            if (_rejectButton != null)
            {
                _rejectButton.SetEnabled(hasSelection);
                _rejectButton.tooltip = hasSelection ? "Reject selected order" : "Select an order first";
            }
        }

        private PendingApprovalRow? SelectedRow()
        {
            if (_approvalList == null || _approvalList.selectedIndex < 0
                || _approvalList.selectedIndex >= _rows.Count)
            {
                return null;
            }

            return _rows[_approvalList.selectedIndex];
        }

        private void SetStatus(string? status)
        {
            _lastStatus = status;
            if (_statusLine != null)
            {
                _statusLine.text = status ?? string.Empty;
            }
        }
    }
}
#endif
