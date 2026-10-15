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
        private int _lastRosterVersion;

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
            Refresh(force: true);
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
                _rosterList.makeItem = MakeRosterRow;
                _rosterList.bindItem = BindRosterRow;
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

        private static VisualElement MakeRosterRow()
        {
            var row = new VisualElement();
            row.AddToClassList("agent-roster-row");
            var badge = new Label { name = "agent-roster-badge" };
            badge.AddToClassList("agent-roster-badge");
            var line = new Label { name = "agent-roster-line" };
            line.AddToClassList("agent-roster-line");
            row.Add(badge);
            row.Add(line);
            return row;
        }

        private void BindRosterRow(VisualElement element, int index)
        {
            var badgeLabel = element.Q<Label>("agent-roster-badge");
            var lineLabel = element.Q<Label>("agent-roster-line");
            if (index < 0 || index >= _presentation.Rows.Count)
            {
                if (lineLabel != null)
                {
                    lineLabel.text = string.Empty;
                    lineLabel.tooltip = string.Empty;
                }

                ClearBadge(badgeLabel);
                return;
            }

            var row = _presentation.Rows[index];
            if (lineLabel != null)
            {
                lineLabel.text = row.DisplayLine;
                // S109-02: accessible attention label (not color-only).
                lineLabel.tooltip = row.AccessibleAttentionLabel;
            }

            if (badgeLabel == null)
            {
                return;
            }

            var badge = row.Badge;
            if (badge is null)
            {
                ClearBadge(badgeLabel);
                return;
            }

            // DRG-375: shape, short text, colour token, and accessible name are independent channels.
            badgeLabel.text = badge.ShortText;
            badgeLabel.tooltip = badge.AccessibleLabel;
            badgeLabel.ClearClassList();
            badgeLabel.AddToClassList("agent-roster-badge");
            badgeLabel.AddToClassList(badge.ColorToken);
            badgeLabel.AddToClassList(ShapeClass(badge.Shape));
        }

        private static void ClearBadge(Label? badgeLabel)
        {
            if (badgeLabel == null)
            {
                return;
            }

            badgeLabel.text = string.Empty;
            badgeLabel.tooltip = string.Empty;
            badgeLabel.ClearClassList();
            badgeLabel.AddToClassList("agent-roster-badge");
        }

        private static string ShapeClass(string shape) => shape switch
        {
            "square" => "badge-shape-square",
            "diamond" => "badge-shape-diamond",
            "diamond-dashed" => "badge-shape-diamond-dashed",
            _ => "badge-shape-none",
        };

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

            // Only mark registered when at least one button was wired.
            // If _directiveButtons is empty (UXML not ready), leave false so the next
            // TryWireElements call can retry once buttons are available.
            if (_directiveButtons.Count > 0)
            {
                _handlersRegistered = true;
            }
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
                Refresh(force: true);
                return;
            }

            SetStatus(reason ?? "ISSUE_FAILED");
        }

        private void Refresh(bool force = false)
        {
            if (!_wired || bridgeHost == null)
            {
                return;
            }

            // Rebuild presentation only when roster content changes (avoid per-frame allocation).
            var roster = bridgeHost.LastAgentRoster;
            var version = ComputeRosterVersion(roster);
            if (!force && version == _lastRosterVersion)
            {
                // Still update button enablement / status which can change without roster mutation.
                UpdateDirectiveButtons();
                return;
            }

            _lastRosterVersion = version;
            _presentation = AgentRosterApplyState.Apply(roster);
            if (_rosterList != null)
            {
                _rosterList.itemsSource = _presentation.Lines as System.Collections.IList
                    ?? new List<string>(_presentation.Lines);
                _rosterList.Rebuild();
            }

            UpdateDirectiveButtons();

            var root = _document.rootVisualElement?.Q(RootName);
            if (root != null)
            {
                root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void UpdateDirectiveButtons()
        {
            if (bridgeHost == null)
            {
                return;
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
        }

        private static int ComputeRosterVersion(IReadOnlyList<AgentRosterEntry> roster)
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + roster.Count;
                for (var i = 0; i < roster.Count; i++)
                {
                    var e = roster[i];
                    hash = hash * 31 + (e.UnitId?.GetHashCode() ?? 0);
                    hash = hash * 31 + (e.AgentId?.GetHashCode() ?? 0);
                    hash = hash * 31 + (e.ModeLabel?.GetHashCode() ?? 0);
                    hash = hash * 31 + (e.StatusLabel?.GetHashCode() ?? 0);
                    hash = hash * 31 + (e.AttentionLabel?.GetHashCode() ?? 0);
                }
                return hash;
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
