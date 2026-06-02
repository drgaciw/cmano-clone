// Mission list left-drawer tab — UI Toolkit panel bound to DelegationBridgeHost.
#if UNITY_5_3_OR_NEWER
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MissionListPanelHost : MonoBehaviour
    {
        private const string RootName = "mission-list-root";
        private const string ListName = "mission-list";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private ListView? _missionList;
        private MissionListPanelState _panelState = new(Array.Empty<MissionListDisplayRow>());
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

            _missionList = root.Q<ListView>(ListName);
            if (_missionList != null)
            {
                _missionList.makeItem = () => new Label();
                _missionList.bindItem = (element, index) =>
                {
                    if (element is Label label && index >= 0 && index < _panelState.MissionRows.Count)
                    {
                        label.text = _panelState.MissionRows[index].DisplayLine;
                    }
                };
                _wired = true;
            }
        }

        private void Refresh()
        {
            if (bridgeHost == null || _missionList == null)
            {
                return;
            }

            _panelState = MissionListPanelBinder.Bind(bridgeHost.LastMissionList);
            _missionList.itemsSource = _panelState.MissionRows.ToList();
            _missionList.Rebuild();

            var root = _document.rootVisualElement?.Q(RootName);
            if (root != null)
            {
                root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
#endif