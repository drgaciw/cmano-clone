// CMD-25 Boat Operations panel — LOG-09…11 launch/recover/abort UI.
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class BoatOpsPanelHost : MonoBehaviour
    {
        private const string RootName = "boat-ops-root";
        private const string HeaderName = "boat-ops-header";
        private const string EmptyName = "boat-ops-empty";
        private const string ListName = "boat-ops-list";
        private const string LaunchButtonName = "boat-ops-launch";
        private const string RecoverButtonName = "boat-ops-recover";
        private const string AbortButtonName = "boat-ops-abort";
        private const string StatusName = "boat-ops-status";

        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _headerLine;
        private Label? _emptyLine;
        private Label? _statusLine;
        private ListView? _craftList;
        private Button? _launchButton;
        private Button? _recoverButton;
        private Button? _abortButton;
        private BoatOpsPresentation _presentation = BoatOpsPresentation.Empty;
        private bool _wired;
        private bool _handlersRegistered;
        private string? _lastStatus;

        /// <summary>Optional external presentation source (session / bridge).</summary>
        public System.Func<BoatOpsPresentation>? PresentationSource { get; set; }

        /// <summary>Optional launch handler; returns false + reason on refuse.</summary>
        public System.Func<string, (bool ok, string? reason)>? OnLaunch { get; set; }

        /// <summary>Optional recover handler.</summary>
        public System.Func<string, (bool ok, string? reason)>? OnRecover { get; set; }

        /// <summary>Optional abort handler.</summary>
        public System.Func<string, (bool ok, string? reason)>? OnAbort { get; set; }

        /// <summary>Last applied Boat Ops presentation (CMD-25).</summary>
        public BoatOpsPresentation LastPresentation => _presentation;

        /// <summary>Last launch/recover/abort status reason shown on the panel.</summary>
        public string? LastStatus => _lastStatus;

        private void Reset()
        {
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
            if (!showPanel)
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
            _emptyLine = panel.Q<Label>(EmptyName);
            _statusLine = panel.Q<Label>(StatusName);
            _craftList = panel.Q<ListView>(ListName);
            _launchButton = panel.Q<Button>(LaunchButtonName);
            _recoverButton = panel.Q<Button>(RecoverButtonName);
            _abortButton = panel.Q<Button>(AbortButtonName);

            if (_craftList != null)
            {
                _craftList.makeItem = () => new Label();
                _craftList.bindItem = (element, index) =>
                {
                    if (element is Label label && index >= 0 && index < _presentation.Lines.Count)
                    {
                        label.text = _presentation.Lines[index];
                    }
                };
                _craftList.selectionType = SelectionType.Single;
                _craftList.selectionChanged -= OnCraftSelectionChanged;
                _craftList.selectionChanged += OnCraftSelectionChanged;
            }

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }

            WireClickHandlers();
            _wired = _headerLine != null || _craftList != null
                || _launchButton != null || _recoverButton != null || _abortButton != null;
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

            if (_recoverButton != null)
            {
                _recoverButton.clicked += OnRecoverClicked;
            }

            if (_abortButton != null)
            {
                _abortButton.clicked += OnAbortClicked;
            }

            _handlersRegistered = true;
        }

        private void OnCraftSelectionChanged(IEnumerable<object> _)
        {
            UpdateActionButtons();
        }

        private void OnLaunchClicked()
        {
            var row = ResolveSelectedRow();
            if (row == null)
            {
                SetStatus("NO_SELECTION");
                return;
            }

            if (OnLaunch == null)
            {
                SetStatus(row.CanLaunch ? "NO_HANDLER" : row.LaunchDisabledReason ?? "LAUNCH_UNAVAILABLE");
                return;
            }

            var (ok, reason) = OnLaunch(row.CraftId);
            if (ok)
            {
                SetStatus(null);
                Refresh();
                return;
            }

            SetStatus(reason ?? "LAUNCH_FAILED");
        }

        private void OnRecoverClicked()
        {
            var row = ResolveSelectedRow();
            if (row == null)
            {
                SetStatus("NO_SELECTION");
                return;
            }

            if (OnRecover == null)
            {
                SetStatus(row.CanRecover ? "NO_HANDLER" : row.RecoverDisabledReason ?? "RECOVER_UNAVAILABLE");
                return;
            }

            var (ok, reason) = OnRecover(row.CraftId);
            if (ok)
            {
                SetStatus(null);
                Refresh();
                return;
            }

            SetStatus(reason ?? "RECOVER_FAILED");
        }

        private void OnAbortClicked()
        {
            var row = ResolveSelectedRow();
            if (row == null)
            {
                SetStatus("NO_SELECTION");
                return;
            }

            if (OnAbort == null)
            {
                SetStatus(row.CanAbort ? "NO_HANDLER" : "ABORT_UNAVAILABLE");
                return;
            }

            var (ok, reason) = OnAbort(row.CraftId);
            if (ok)
            {
                SetStatus(null);
                Refresh();
                return;
            }

            SetStatus(reason ?? "ABORT_FAILED");
        }

        /// <summary>Apply presentation via headless apply-state (tests / direct bind).</summary>
        public void ApplyPresentation(BoatOpsPresentation presentation)
        {
            _presentation = presentation ?? BoatOpsPresentation.Empty;
            ApplyPresentationToUi();
        }

        private void Refresh()
        {
            if (!_wired)
            {
                return;
            }

            if (PresentationSource != null)
            {
                _presentation = PresentationSource() ?? BoatOpsPresentation.Empty;
            }

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

            if (_craftList != null)
            {
                _craftList.itemsSource = _presentation.Lines as System.Collections.IList
                    ?? new List<string>(_presentation.Lines);
                _craftList.Rebuild();
            }

            UpdateActionButtons();
        }

        private void UpdateActionButtons()
        {
            var row = ResolveSelectedRow();
            var canLaunch = row?.CanLaunch == true;
            var canRecover = row?.CanRecover == true;
            var canAbort = row?.CanAbort == true;

            if (_launchButton != null)
            {
                _launchButton.SetEnabled(canLaunch);
                _launchButton.tooltip = canLaunch
                    ? "Launch boat"
                    : row?.LaunchDisabledReason ?? "Launch unavailable";
            }

            if (_recoverButton != null)
            {
                _recoverButton.SetEnabled(canRecover);
                _recoverButton.tooltip = canRecover
                    ? "Recover boat"
                    : row?.RecoverDisabledReason ?? "Recover unavailable";
            }

            if (_abortButton != null)
            {
                _abortButton.SetEnabled(canAbort);
                _abortButton.tooltip = canAbort
                    ? "Abort launch"
                    : "Abort unavailable";
            }
        }

        private BoatOpsDisplayRow? ResolveSelectedRow()
        {
            if (_presentation.Rows.Count == 0)
            {
                return null;
            }

            if (_craftList != null && _craftList.selectedIndex >= 0
                && _craftList.selectedIndex < _presentation.Rows.Count)
            {
                return _presentation.Rows[_craftList.selectedIndex];
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
