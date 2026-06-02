// OOB left-drawer slice — UI Toolkit panel bound to DelegationBridgeHost.
#if UNITY_5_3_OR_NEWER
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class OobTreePanelHost : MonoBehaviour
    {
        private const string RootName = "oob-tree-root";
        private const string OobListName = "oob-list";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private ListView? _oobList;
        private OobTreePanelState _panelState = new(Array.Empty<OobTreeDisplayRow>());
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

            _oobList = root.Q<ListView>(OobListName);
            if (_oobList != null)
            {
                _oobList.makeItem = () => new Label();
                _oobList.bindItem = (element, index) =>
                {
                    if (element is Label label && index >= 0 && index < _panelState.UnitRows.Count)
                    {
                        label.text = _panelState.UnitRows[index].DisplayLine;
                    }
                };
                _wired = true;
            }
        }

        private void Refresh()
        {
            if (bridgeHost == null)
            {
                return;
            }

            _panelState = OobTreePanelBinder.Bind(bridgeHost.LastOobTree, bridgeHost.SelectedUnitId);
            if (_oobList != null)
            {
                _oobList.itemsSource = _panelState.UnitRows;
                _oobList.Rebuild();
            }

            var root = _document.rootVisualElement?.Q(RootName);
            if (root != null)
            {
                root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
#endif