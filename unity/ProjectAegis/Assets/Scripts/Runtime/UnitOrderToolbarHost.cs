// CMD-16 unit order toolbar shell — issues player commands via DelegationBridgeHost (CMD-31).
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class UnitOrderToolbarHost : MonoBehaviour
    {
        private const string RootName = "unit-order-toolbar-root";
        private const string StatusName = "order-status-line";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _statusLine;
        private readonly Dictionary<string, Button> _orderButtons = new();
        private bool _wired;
        private bool _handlersRegistered;
        private string? _lastStatus;

        /// <summary>Last status / failure reason shown on the toolbar.</summary>
        public string? LastStatus => _lastStatus;

        private static readonly (string ButtonName, string CommandId, string Label)[] ButtonDefs =
        {
            ("order-hold", "hold", "Hold"),
            ("order-rtb", "rtb", "RTB"),
            ("order-plot-course", "plot_course", "Plot Course"),
            ("order-emcon", "set_emcon", "EMCON"),
            ("order-sensors", "set_sensors", "Sensors"),
            ("order-engage", "engage", "Engage"),
        };

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
            _statusLine = panel.Q<Label>(StatusName);
            _orderButtons.Clear();
            foreach (var (buttonName, commandId, _) in ButtonDefs)
            {
                var button = panel.Q<Button>(buttonName);
                if (button != null)
                {
                    _orderButtons[commandId] = button;
                }
            }

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }

            WireClickHandlers();
            _wired = _orderButtons.Count > 0;
        }

        private void WireClickHandlers()
        {
            if (_handlersRegistered)
            {
                return;
            }

            foreach (var (commandId, button) in _orderButtons)
            {
                var captured = commandId;
                button.clicked += () => OnOrderClicked(captured);
            }

            _handlersRegistered = true;
        }

        private void OnOrderClicked(string commandId)
        {
            if (bridgeHost == null)
            {
                SetStatus("NO_BRIDGE");
                return;
            }

            if (bridgeHost.TryIssueSelectedCommand(commandId, out var reason))
            {
                SetStatus(null);
                Refresh();
                return;
            }

            SetStatus(reason ?? "ISSUE_FAILED");
        }

        private void Refresh()
        {
            if (!_wired || bridgeHost == null)
            {
                return;
            }

            var hasSelection = !string.IsNullOrEmpty(bridgeHost.SelectedUnitId);
            var disableReason = hasSelection ? null : "NO_SELECTION";

            foreach (var (commandId, button) in _orderButtons)
            {
                var enabled = hasSelection;
                button.SetEnabled(enabled);
                button.tooltip = enabled
                    ? commandId
                    : disableReason ?? "Blocked";
            }

            if (!hasSelection && string.IsNullOrEmpty(_lastStatus))
            {
                SetStatus("NO_SELECTION");
            }
            else if (hasSelection && _lastStatus == "NO_SELECTION")
            {
                SetStatus(null);
            }

            var root = _document.rootVisualElement?.Q(RootName);
            if (root != null)
            {
                root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
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
