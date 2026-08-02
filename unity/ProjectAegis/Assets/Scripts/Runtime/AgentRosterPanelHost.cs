// CMD-37 agent roster + directive issuance surface — UI Toolkit bound to DelegationBridgeHost.
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using ProjectAegis.Delegation.Input;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class AgentRosterPanelHost : MonoBehaviour
    {
        private const string RootName = "agent-roster-root";
        private const string ListName = "agent-roster-list";
        private const string StatusName = "agent-directive-status";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private ListView? _rosterList;
        private Label? _statusLine;
        private readonly Dictionary<string, Button> _directiveButtons = new();
        private AgentRosterPresentation _presentation = AgentRosterPresentation.Empty;
        private bool _wired;
        private bool _handlersRegistered;
        private string? _lastStatus;

        /// <summary>Last status / failure reason shown on the panel.</summary>
        public string? LastStatus => _lastStatus;

        /// <summary>Last applied roster presentation (for tests).</summary>
        public AgentRosterPresentation LastPresentation => _presentation;

        private static readonly (string ButtonName, string DirectiveId)[] ButtonDefs =
        {
            ("directive-take-control", AgentDirectiveIssuance.TakeControlId),
            ("directive-return-to-agent", AgentDirectiveIssuance.ReturnToAgentId),
            ("directive-hold", AgentDirectiveIssuance.HoldId),
            ("directive-rtb", AgentDirectiveIssuance.RtbId),
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

            // Prefer host-maintained roster; if empty, rebuild from registry bindings.
            if (bridgeHost.LastAgentRoster.Count == 0)
            {
                bridgeHost.RefreshAgentRoster();
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
            _rosterList = panel.Q<ListView>(ListName);
            _directiveButtons.Clear();
            foreach (var (buttonName, directiveId) in ButtonDefs)
            {
                var button = panel.Q<Button>(buttonName);
                if (button != null)
                {
                    _directiveButtons[directiveId] = button;
                }
            }

            if (_rosterList != null)
            {
                _rosterList.makeItem = () => new Label();
                _rosterList.bindItem = (element, index) =>
                {
                    if (element is Label label && index >= 0 && index < _presentation.Lines.Count)
                    {
                        label.text = _presentation.Lines[index];
                    }
                };
                _rosterList.selectionType = SelectionType.Single;
                _rosterList.selectionChanged -= OnRosterSelectionChanged;
                _rosterList.selectionChanged += OnRosterSelectionChanged;
            }

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }

            WireClickHandlers();
            _wired = _rosterList != null || _directiveButtons.Count > 0;
        }

        private void WireClickHandlers()
        {
            if (_handlersRegistered)
            {
                return;
            }

            foreach (var (directiveId, button) in _directiveButtons)
            {
                var captured = directiveId;
                button.clicked += () => OnDirectiveClicked(captured);
            }

            _handlersRegistered = true;
        }

        private void OnRosterSelectionChanged(IEnumerable<object> _)
        {
            if (_rosterList == null || bridgeHost == null)
            {
                return;
            }

            var index = _rosterList.selectedIndex;
            if (index < 0 || index >= _presentation.Rows.Count)
            {
                return;
            }

            bridgeHost.SelectUnit(_presentation.Rows[index].UnitId);
        }

        private void OnDirectiveClicked(string directiveId)
        {
            if (bridgeHost == null)
            {
                SetStatus("NO_BRIDGE");
                return;
            }

            if (bridgeHost.TryIssueAgentDirective(directiveId, out var reason))
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

            _presentation = AgentRosterApplyState.Apply(bridgeHost.LastAgentRoster);
            if (_rosterList != null)
            {
                _rosterList.itemsSource = _presentation.Lines as System.Collections.IList
                    ?? new List<string>(_presentation.Lines);
                _rosterList.Rebuild();
            }

            var hasSelection = !string.IsNullOrEmpty(bridgeHost.SelectedUnitId);
            var hasActiveAgent = bridgeHost.SelectedUnitHasActiveAgent();
            var hasSuspendedAgent = bridgeHost.SelectedUnitHasSuspendedAgent();

            foreach (var (directiveId, button) in _directiveButtons)
            {
                var validation = AgentDirectiveIssuance.Validate(
                    directiveId,
                    hasSelection,
                    hasActiveAgent,
                    hasSuspendedAgent);
                button.SetEnabled(validation.Ok);
                button.tooltip = validation.Ok
                    ? directiveId
                    : validation.FailureReason ?? "Blocked";
            }

            if (!hasSelection && string.IsNullOrEmpty(_lastStatus))
            {
                SetStatus(AgentDirectiveIssuance.ReasonNoSelection);
            }
            else if (hasSelection && _lastStatus == AgentDirectiveIssuance.ReasonNoSelection)
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
