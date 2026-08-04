// CMD-24 Air Operations panel — Phase A readiness + Phase N launch/abort UI (LOG-08).
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class AirOpsPanelHost : MonoBehaviour
    {
        private const string RootName = "air-ops-root";
        private const string HeaderName = "air-ops-header";
        private const string EmptyName = "air-ops-empty";
        private const string ListName = "air-ops-list";
        private const string LaunchButtonName = "air-ops-launch";
        private const string AbortButtonName = "air-ops-abort";
        private const string StatusName = "air-ops-status";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _headerLine;
        private Label? _emptyLine;
        private Label? _statusLine;
        private ListView? _assetList;
        private Button? _launchButton;
        private Button? _abortButton;
        private AirOpsPresentation _presentation = AirOpsPresentation.Empty;
        private bool _wired;
        private bool _handlersRegistered;
        private string? _lastStatus;

        /// <summary>Last applied Air Ops presentation (CMD-24).</summary>
        public AirOpsPresentation LastPresentation => _presentation;

        /// <summary>Last launch/abort status reason shown on the panel.</summary>
        public string? LastStatus => _lastStatus;

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

            // Prefer host-maintained list; rebuild from Session + OOB when needed.
            bridgeHost.RefreshAirOps();
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
            _emptyLine = panel.Q<Label>(EmptyName);
            _statusLine = panel.Q<Label>(StatusName);
            _assetList = panel.Q<ListView>(ListName);
            _launchButton = panel.Q<Button>(LaunchButtonName);
            _abortButton = panel.Q<Button>(AbortButtonName);

            if (_assetList != null)
            {
                _assetList.makeItem = () => new Label();
                _assetList.bindItem = (element, index) =>
                {
                    if (element is Label label && index >= 0 && index < _presentation.Lines.Count)
                    {
                        label.text = _presentation.Lines[index];
                    }
                };
                _assetList.selectionType = SelectionType.Single;
                _assetList.selectionChanged -= OnAssetSelectionChanged;
                _assetList.selectionChanged += OnAssetSelectionChanged;
            }

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }

            WireClickHandlers();
            _wired = _headerLine != null || _assetList != null
                || _launchButton != null || _abortButton != null;
        }

        private void WireClickHandlers()
        {
            if (_handlersRegistered)
            {
                return;
            }

            if (_launchButton != null)
            {
                _launchButton.clicked += OnLaunchClicked;
            }

            if (_abortButton != null)
            {
                _abortButton.clicked += OnAbortClicked;
            }

            _handlersRegistered = true;
        }

        private void OnAssetSelectionChanged(IEnumerable<object> _)
        {
            if (_assetList == null || bridgeHost == null)
            {
                return;
            }

            var index = _assetList.selectedIndex;
            if (index < 0 || index >= _presentation.Rows.Count)
            {
                return;
            }

            bridgeHost.SelectUnit(_presentation.Rows[index].UnitId);
            UpdateActionButtons();
        }

        private void OnLaunchClicked()
        {
            if (bridgeHost == null)
            {
                SetStatus("NO_BRIDGE");
                return;
            }

            if (bridgeHost.TryLaunchSelectedAircraft(out var reason))
            {
                SetStatus(null);
                bridgeHost.RefreshAirOps();
                Refresh();
                return;
            }

            SetStatus(reason ?? "LAUNCH_FAILED");
        }

        private void OnAbortClicked()
        {
            if (bridgeHost == null)
            {
                SetStatus("NO_BRIDGE");
                return;
            }

            if (bridgeHost.TryAbortLaunchSelected(out var reason))
            {
                SetStatus(null);
                bridgeHost.RefreshAirOps();
                Refresh();
                return;
            }

            SetStatus(reason ?? "ABORT_FAILED");
        }

        /// <summary>Apply presentation via headless apply-state (tests / direct bind).</summary>
        public void ApplyPresentation(AirOpsPresentation presentation)
        {
            _presentation = presentation ?? AirOpsPresentation.Empty;
            ApplyPresentationToUi();
        }

        private void Refresh()
        {
            if (!_wired || bridgeHost == null)
            {
                return;
            }

            _presentation = AirOpsApplyState.Apply(
                bridgeHost.LastAirOps,
                bridgeHost.HasAirOpsReadinessData);

            ApplyPresentationToUi();

            var root = _document.rootVisualElement?.Q(RootName);
            if (root != null)
            {
                root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void ApplyPresentationToUi()
        {
            if (_headerLine != null)
            {
                _headerLine.text = _presentation.HeaderLine;
            }

            if (_emptyLine != null)
            {
                _emptyLine.text = _presentation.EmptyStateLine ?? string.Empty;
                _emptyLine.style.display = string.IsNullOrEmpty(_presentation.EmptyStateLine)
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;
            }

            if (_assetList != null)
            {
                _assetList.itemsSource = _presentation.Lines as System.Collections.IList
                    ?? new List<string>(_presentation.Lines);
                _assetList.Rebuild();
            }

            UpdateActionButtons();
        }

        private void UpdateActionButtons()
        {
            var row = ResolveSelectedRow();
            var canLaunch = row?.CanLaunch == true;
            var canAbort = row?.CanAbort == true;

            if (_launchButton != null)
            {
                _launchButton.SetEnabled(canLaunch);
                _launchButton.tooltip = canLaunch
                    ? "Launch aircraft"
                    : row?.LaunchDisabledReason ?? "Launch unavailable";
            }

            if (_abortButton != null)
            {
                _abortButton.SetEnabled(canAbort);
                _abortButton.tooltip = canAbort
                    ? "Abort launch"
                    : "Abort unavailable";
            }
        }

        private AirOpsDisplayRow? ResolveSelectedRow()
        {
            if (_presentation.Rows.Count == 0)
            {
                return null;
            }

            if (_assetList != null && _assetList.selectedIndex >= 0
                && _assetList.selectedIndex < _presentation.Rows.Count)
            {
                return _presentation.Rows[_assetList.selectedIndex];
            }

            if (bridgeHost != null && !string.IsNullOrEmpty(bridgeHost.SelectedUnitId))
            {
                foreach (var r in _presentation.Rows)
                {
                    if (r.UnitId == bridgeHost.SelectedUnitId)
                    {
                        return r;
                    }
                }
            }

            return null;
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
