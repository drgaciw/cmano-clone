// Combat message log strip — UI Toolkit panel bound to DelegationBridgeHost.
// S104-02: row selection exposes sequenceId + unitId (CMD-05 depth).
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

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;
        [SerializeField] private int maxRows = 12;

        private UIDocument _document = null!;
        private ListView? _messageList;
        private MessageLogPanelState _panelState = new(Array.Empty<MessageLogDisplayRow>());
        private bool _wired;

        /// <summary>Last selected order-log sequence id (null if none).</summary>
        public ulong? SelectedSequenceId { get; private set; }

        /// <summary>Last selected unit id from message log row (null if none).</summary>
        public string? SelectedUnitId { get; private set; }

        /// <summary>Current bound panel rows (for tests).</summary>
        public MessageLogPanelState PanelState => _panelState;

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
                    label.AddToClassList("message-log-row--selectable");
                    AddCategoryClass(label, row.Category);
                };
                _messageList.selectionType = SelectionType.Single;
                _messageList.selectionChanged -= OnMessageSelectionChanged;
                _messageList.selectionChanged += OnMessageSelectionChanged;
            }

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }

            panel.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnMessageSelectionChanged(System.Collections.Generic.IEnumerable<object> _)
        {
            if (_messageList == null)
            {
                return;
            }

            var index = _messageList.selectedIndex;
            ApplySelectionIndex(index);
        }

        /// <summary>Headless-friendly selection entry (also used by host selection callback).</summary>
        public bool TrySelectRow(int index)
        {
            var selection = MessageLogPanelSelection.SelectRow(_panelState, index);
            if (selection == null)
            {
                SelectedSequenceId = null;
                SelectedUnitId = null;
                return false;
            }

            SelectedSequenceId = selection.SequenceId;
            SelectedUnitId = selection.UnitId;

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
                return;
            }

            var lines = bridgeHost.LastMessageLog;
            if (maxRows > 0 && lines.Count > maxRows)
            {
                lines = lines.Skip(lines.Count - maxRows).ToArray();
            }

            _panelState = MessageLogPanelBinder.Bind(lines);
            _messageList.itemsSource = _panelState.Rows.ToList();
            _messageList.Rebuild();
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
    }
}
#endif
