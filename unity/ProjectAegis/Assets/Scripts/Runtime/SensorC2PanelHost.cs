// Sensor C2 left-drawer slice — UI Toolkit panel bound to DelegationBridgeHost.
#if UNITY_5_3_OR_NEWER
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class SensorC2PanelHost : MonoBehaviour
    {
        private const string RootName = "sensor-c2-root";
        private const string EmconName = "emcon-label";
        private const string TrackName = "track-label";
        private const string ContactCountName = "contact-count-label";
        private const string ContactListName = "contact-list";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _emconLabel;
        private Label? _trackLabel;
        private Label? _contactCountLabel;
        private ListView? _contactList;
        private SensorC2PanelState _panelState = new("EMCON: —", "TRACK: —", "CONTACTS: 0", Array.Empty<SensorC2ContactRow>());
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
            _emconLabel = panel.Q<Label>(EmconName);
            _trackLabel = panel.Q<Label>(TrackName);
            _contactCountLabel = panel.Q<Label>(ContactCountName);
            _contactList = panel.Q<ListView>(ContactListName);
            _wired = _emconLabel != null && _trackLabel != null && _contactCountLabel != null && _contactList != null;

            if (_contactList != null)
            {
                _contactList.makeItem = () =>
                {
                    var label = new Label();
                    label.AddToClassList("sensor-c2-contact-row");
                    return label;
                };
                _contactList.bindItem = (element, index) =>
                {
                    if (element is not Label label || index < 0 || index >= _panelState.ContactRows.Count)
                    {
                        return;
                    }

                    var row = _panelState.ContactRows[index];
                    label.text = row.DisplayLine;
                    label.ClearClassList();
                    label.AddToClassList("sensor-c2-contact-row");
                    AddLifecycleClass(label, row.LifecycleState);
                };
                _contactList.selectionType = SelectionType.None;
            }

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }

            panel.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Refresh()
        {
            if (!_wired || bridgeHost == null || _contactList == null)
            {
                return;
            }

            _panelState = SensorC2PanelBinder.Bind(bridgeHost.LastSensorC2);
            _emconLabel!.text = _panelState.EmconLabel;
            _trackLabel!.text = _panelState.TrackLabel;
            _contactCountLabel!.text = _panelState.ContactCountLabel;
            _contactList.itemsSource = _panelState.ContactRows.ToList();
            _contactList.Rebuild();
        }

        private static void AddLifecycleClass(VisualElement element, string lifecycleState)
        {
            switch (lifecycleState)
            {
                case "Classified":
                    element.AddToClassList("sensor-c2-contact-row--classified");
                    break;
                case "Identified":
                    element.AddToClassList("sensor-c2-contact-row--identified");
                    break;
                case "Lost":
                    element.AddToClassList("sensor-c2-contact-row--lost");
                    break;
            }
        }
    }
}
#endif