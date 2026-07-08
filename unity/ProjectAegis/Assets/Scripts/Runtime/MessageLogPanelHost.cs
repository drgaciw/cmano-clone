// Combat message log strip — UI Toolkit panel bound to DelegationBridgeHost.
#if UNITY_5_3_OR_NEWER
using System.Linq;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
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

        /// <summary>Estimated single-line row height (px) at 100% scale — used only to seed
        /// fixed-height virtualization; not a pixel-perfect layout guarantee (req 20 AC-10).</summary>
        private const float FixedRowHeightPx = 16f;

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;
        [SerializeField] private int maxRows = 12;

        /// <summary>req 20 AC-1 / F5 — text scale tier (accessibility-requirements.md §3). Default
        /// 100% until a settings UI ships.</summary>
        [SerializeField] private C2TextScalePercent scalePercent = C2AccessibilitySettings.DefaultScalePercent;

        private UIDocument _document = null!;
        private ListView? _messageList;
        private MessageLogPanelState _panelState = new(Array.Empty<MessageLogDisplayRow>());
        private readonly PanelRefreshGate<MessageLogDisplayRow> _refreshGate = new();
        private bool _wired;

        private void Reset()
        {
            if (bridgeHost == null)
            {
                bridgeHost = GetComponent<DelegationBridgeHost>();
            }

            _document = GetComponent<UIDocument>();
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
            _messageList = panel.Q<ListView>(ListName);
            _wired = _messageList != null;

            if (_messageList != null)
            {
                _messageList.makeItem = () =>
                {
                    var label = new Label();
                    label.AddToClassList("message-log-row");
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
                    label.ClearClassList();
                    label.AddToClassList("message-log-row");
                    AddCategoryClass(label, row.Category);
                    AddOrderStateClass(label, row.LifecycleState); // T2: req 20 §Order lifecycle
                };
                _messageList.selectionType = SelectionType.None;

                // req 20 AC-10 / NFR virtualization — explicit fixed-height virtualization so
                // Unity only realizes viewport (+ pool) elements at 5k+ rows instead of the whole
                // list (see also the itemsSource/Rebuild dirty-gate in Refresh()).
                _messageList.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
                _messageList.fixedItemHeight = FixedRowHeightPx;
            }

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }

            ApplyAccessibilityScale(panel);

            panel.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>req 20 AC-1 / F5 — toggles the AegisTokens.uss scale-tier class matching
        /// <see cref="scalePercent"/> onto the panel root; font-size is inherited from there down
        /// to every ".message-log-row" Label (accessibility-requirements.md §3).</summary>
        private void ApplyAccessibilityScale(VisualElement panel)
        {
            panel.RemoveFromClassList("aegis-scale-125");
            panel.RemoveFromClassList("aegis-scale-150");

            var scaleClass = C2AccessibilitySettings.ScaleUssClass(scalePercent);
            if (scaleClass != null)
            {
                panel.AddToClassList(scaleClass);
            }
        }

        private void Refresh()
        {
            if (!_wired || bridgeHost == null || _messageList == null)
            {
                return;
            }

            var lines = bridgeHost.LastMessageLog;
            if (maxRows > 0 && lines.Count > maxRows)
            {
                lines = lines.Skip(lines.Count - maxRows).ToArray();
            }

            // T2: req 20 §Order lifecycle — surface each PLAYER_ORDER row's resolved lifecycle state.
            var lifecycle = OrderLifecycleProjection.Project(bridgeHost.Bridge.Orchestrator.DecisionLog);
            var candidate = MessageLogPanelBinder.Bind(lines, lifecycle);

            // req 20 AC-10 — skip itemsSource reassignment + Rebuild() when the bound snapshot is
            // structurally unchanged from the last frame that was actually pushed to the
            // ListView. Rebuild() tears down pooled/visible elements; doing that unconditionally
            // every LateUpdate defeats ListView's recycling virtualization at scale.
            if (!_refreshGate.IsDirty(candidate.Rows))
            {
                return;
            }

            _panelState = candidate;
            _messageList.itemsSource = _panelState.Rows.ToList();
            _messageList.Rebuild();
            _refreshGate.MarkApplied(_panelState.Rows);
        }

        private static void AddCategoryClass(VisualElement element, string category)
        {
            switch (category)
            {
                case "KILL_CONFIRMED":
                case "HIT":
                    element.AddToClassList("message-log-row--kill");
                    break;
                case "MAGAZINE":
                    element.AddToClassList("message-log-row--magazine");
                    break;
                case "COMMS":
                    element.AddToClassList("message-log-row--comms");
                    break;
                case "CONTACT":
                case "CONTACT_CHANGE":
                    element.AddToClassList("message-log-row--contact");
                    break;
                case "MISSION":
                case "MISSION_TRANSITION":
                    element.AddToClassList("message-log-row--mission");
                    break;
            }
        }

        /// <summary>T2 (req 20 §Order lifecycle): tint PLAYER_ORDER rows with the Phase 0
        /// .order-state--* USS class + icon/text tooltip (AA — colour is a secondary cue only).</summary>
        private static void AddOrderStateClass(VisualElement element, OrderLifecycleState? lifecycleState)
        {
            if (lifecycleState == null)
            {
                return;
            }

            var chip = OrderStateChipBinder.Bind(lifecycleState.Value);
            if (!string.IsNullOrEmpty(chip.CssClass))
            {
                element.AddToClassList(chip.CssClass);
            }

            element.tooltip = chip.Text;
        }
    }
}
#endif