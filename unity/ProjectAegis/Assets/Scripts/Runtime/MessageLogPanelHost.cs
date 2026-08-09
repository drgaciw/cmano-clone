// Combat message log strip — UI Toolkit panel bound to DelegationBridgeHost.
// S104-02 / S105 A2: row selection by index, sequenceId, unit focus + category class map.
// CMD-23: collapsible body via C2ChromeCollapseState + prefs bag on bridge host.
#if UNITY_5_3_OR_NEWER
using System.Linq;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MessageLogPanelHost : MonoBehaviour
    {
        private const string RootName = "message-log-root";
        private const string ListName = "message-list";
        private const string OnboardingHintName = "message-log-onboarding-hint";
        private const string CollapseToggleName = "message-log-collapse-toggle";
        private const string TitleClass = "message-log-title";
        private const string CollapseToggleClass = "message-log-collapse-toggle";
        private const string CollapsedClass = "message-log-panel--collapsed";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;
        [SerializeField] private int maxRows = 12;

        private UIDocument _document = null!;
        private VisualElement? _panelRoot;
        private ListView? _messageList;
        private VisualElement? _onboardingHint;
        private Button? _collapseToggle;
        private MessageLogPanelState _panelState = new(System.Array.Empty<MessageLogDisplayRow>());
        private bool _wired;

        /// <summary>Last selected order-log sequence id (null if none).</summary>
        public ulong? SelectedSequenceId { get; private set; }

        /// <summary>Last selected unit id from message log row (null if none).</summary>
        public string? SelectedUnitId { get; private set; }

        /// <summary>Last selected row index in the current panel (null if none).</summary>
        public int? SelectedRowIndex { get; private set; }

        /// <summary>Current bound panel rows (for tests).</summary>
        public MessageLogPanelState PanelState => _panelState;

        /// <summary>Last applied chrome collapse presentation (CMD-23).</summary>
        public C2ChromeCollapsePresentation LastChromePresentation { get; private set; } =
            C2ChromeCollapsePresentation.Empty;

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
            if (!_wired)
            {
                TryWireElements();
            }

            if (!showPanel)
            {
                ApplyMasterVisibility();
                return;
            }

            if (bridgeHost == null)
            {
                // Null-safe chrome: expanded defaults under showPanel master visibility.
                ApplyMasterVisibility();
                ApplyChromeCollapse();
                return;
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
            _panelRoot = panel;
            _messageList = panel.Q<ListView>(ListName);
            _onboardingHint = panel.Q(OnboardingHintName);
            EnsureCollapseToggle(panel);
            _wired = _messageList != null;

            if (_messageList != null)
            {
                _messageList.makeItem = () =>
                {
                    var label = new Label();
                    label.focusable = true;
                    label.AddToClassList(MessageLogCategoryClassMap.BaseRowClass);
                    label.RegisterCallback<KeyDownEvent>(OnMessageRowKeyDown);
                    return label;
                };
                _messageList.bindItem = (element, index) =>
                {
                    if (element is not Label label || index < 0 || index >= _panelState.Rows.Count)
                    {
                        return;
                    }

                    var row = _panelState.Rows[index];
                    label.text = row.DisplayLine;
                    label.userData = index;
                    label.ClearClassList();
                    label.AddToClassList(MessageLogCategoryClassMap.BaseRowClass);
                    label.AddToClassList(MessageLogCategoryClassMap.SelectableRowClass);
                    if (!string.IsNullOrEmpty(row.CategoryCssClass))
                    {
                        label.AddToClassList(row.CategoryCssClass);
                    }
                };
                _messageList.selectionType = SelectionType.Single;
                _messageList.selectionChanged -= OnMessageSelectionChanged;
                _messageList.selectionChanged += OnMessageSelectionChanged;
            }

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }

            ApplyMasterVisibility();
            ApplyChromeCollapse();
        }

        private void EnsureCollapseToggle(VisualElement panel)
        {
            _collapseToggle = panel.Q<Button>(CollapseToggleName);
            if (_collapseToggle == null)
            {
                _collapseToggle = new Button
                {
                    name = CollapseToggleName,
                    text = C2ChromeCollapseProjection.CollapseMessageLogLabel,
                };
                _collapseToggle.AddToClassList(CollapseToggleClass);
                _collapseToggle.focusable = true;

                // Prefer a title-row strip: insert after title label when present.
                var title = panel.Q<Label>(className: TitleClass);
                if (title != null)
                {
                    var titleIndex = panel.IndexOf(title);
                    if (titleIndex >= 0)
                    {
                        panel.Insert(titleIndex + 1, _collapseToggle);
                    }
                    else
                    {
                        panel.Insert(0, _collapseToggle);
                    }
                }
                else
                {
                    panel.Insert(0, _collapseToggle);
                }
            }

            _collapseToggle.clicked -= OnCollapseToggleClicked;
            _collapseToggle.clicked += OnCollapseToggleClicked;
        }

        private void OnCollapseToggleClicked()
        {
            // Null-safe: no bridge chrome API → no-op (showPanel remains master visibility).
            if (bridgeHost == null)
            {
                return;
            }

            bridgeHost.ToggleMessageLogCollapsed();
            ApplyChromeCollapse();
        }

        private void OnMessageSelectionChanged(System.Collections.Generic.IEnumerable<object> _)
        {
            if (_messageList == null)
            {
                return;
            }

            ApplySelectionIndex(_messageList.selectedIndex);
        }

        private void OnMessageRowKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode is not (KeyCode.Return or KeyCode.KeypadEnter or KeyCode.Space))
            {
                return;
            }

            if (evt.currentTarget is Label { userData: int index })
            {
                if (_messageList != null)
                {
                    _messageList.SetSelection(index);
                }

                TrySelectRow(index);
                evt.StopPropagation();
            }
        }

        /// <summary>Headless-friendly selection entry (also used by host selection callback).</summary>
        public bool TrySelectRow(int index)
        {
            return ApplySelection(MessageLogPanelSelection.SelectRow(_panelState, index));
        }

        /// <summary>Select by order-log sequence id (CMD-05 deep-link).</summary>
        public bool TrySelectBySequenceId(ulong sequenceId)
        {
            return ApplySelection(MessageLogPanelSelection.SelectBySequenceId(_panelState, sequenceId));
        }

        /// <summary>Unit focus: latest message-log row for unitId, then bridge SelectUnit.</summary>
        public bool TrySelectByUnitId(string unitId)
        {
            return ApplySelection(MessageLogPanelSelection.SelectByUnitId(_panelState, unitId));
        }

        private bool ApplySelection(MessageLogSelection? selection)
        {
            if (selection == null)
            {
                SelectedSequenceId = null;
                SelectedUnitId = null;
                SelectedRowIndex = null;
                return false;
            }

            SelectedSequenceId = selection.SequenceId;
            SelectedUnitId = selection.UnitId;
            SelectedRowIndex = selection.RowIndex;

            if (bridgeHost != null && !string.IsNullOrEmpty(selection.UnitId))
            {
                bridgeHost.SelectUnit(selection.UnitId);
            }

            return true;
        }

        private void ApplySelectionIndex(int index)
        {
            TrySelectRow(index);
        }

        private void Refresh()
        {
            if (!_wired || bridgeHost == null || _messageList == null)
            {
                ApplyMasterVisibility();
                ApplyChromeCollapse();
                return;
            }

            var lines = bridgeHost.LastMessageLog;
            if (maxRows > 0 && lines.Count > maxRows)
            {
                lines = lines.Skip(lines.Count - maxRows).ToArray();
            }

            _panelState = MessageLogPanelBinder.Bind(lines);
            // Skip list rebuild when collapsed (body hidden) to avoid work.
            if (!bridgeHost.ChromeCollapse.MessageLogCollapsed)
            {
                _messageList.itemsSource = _panelState.Rows.ToList();
                _messageList.Rebuild();
            }

            ApplyMasterVisibility();
            ApplyChromeCollapse();
        }

        private void ApplyMasterVisibility()
        {
            if (_panelRoot != null)
            {
                _panelRoot.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>
        /// CMD-23: collapse body to title strip; affordance labels from pure apply-state.
        /// Null-safe when bridge has no chrome API — falls back to expanded body under showPanel.
        /// </summary>
        private void ApplyChromeCollapse()
        {
            var state = bridgeHost != null
                ? bridgeHost.ChromeCollapse
                : C2ChromeCollapseState.Expanded;
            LastChromePresentation = C2ChromeCollapseApplyState.Apply(state);

            var collapsed = showPanel && LastChromePresentation.MessageLogCollapsed;

            if (_messageList != null)
            {
                _messageList.style.display = collapsed ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (_onboardingHint != null)
            {
                _onboardingHint.style.display = collapsed ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (_collapseToggle != null)
            {
                _collapseToggle.text = LastChromePresentation.MessageLogAffordanceLabel;
                _collapseToggle.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_panelRoot != null)
            {
                if (collapsed)
                {
                    _panelRoot.AddToClassList(CollapsedClass);
                }
                else
                {
                    _panelRoot.RemoveFromClassList(CollapsedClass);
                }
            }
        }
    }
}
#endif
