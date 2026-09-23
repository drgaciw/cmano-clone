// OOB left-drawer slice — UI Toolkit panel bound to DelegationBridgeHost.
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class OobTreePanelHost : MonoBehaviour
    {
        private const string RootName = "oob-tree-root";
        private static readonly string OobListName = KeyboardDiscoverySurfaces.OobListElementName;

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

            _oobList = root.Q<ListView>(OobListName);
            if (_oobList != null)
            {
                _oobList.focusable = true;
                _oobList.makeItem = () =>
                {
                    var label = new Label();
                    label.focusable = true;
                    label.RegisterCallback<KeyDownEvent>(OnOobRowKeyDown);
                    return label;
                };
                _oobList.bindItem = (element, index) =>
                {
                    if (element is not Label label || index < 0 || index >= _panelState.UnitRows.Count)
                    {
                        return;
                    }

                    var row = _panelState.UnitRows[index];
                    label.text = row.DisplayLine;
                    label.ClearClassList();
                    label.AddToClassList(row.StyleClass ?? KeyboardDiscoverySurfaces.OobRowBaseClass);
                    if (!row.IsAlive)
                    {
                        label.AddToClassList("oob-row--dead");
                    }

                    label.userData = row.UnitId;
                };
                _oobList.selectionType = SelectionType.Single;
                _oobList.selectionChanged -= OnOobSelectionChanged;
                _oobList.selectionChanged += OnOobSelectionChanged;
                _wired = true;
            }
        }

        private void OnOobSelectionChanged(IEnumerable<object> _)
        {
            if (_oobList == null || bridgeHost == null)
            {
                return;
            }

            var index = _oobList.selectedIndex;
            if (index < 0 || index >= _panelState.UnitRows.Count)
            {
                return;
            }

            bridgeHost.SelectUnit(_panelState.UnitRows[index].UnitId);
        }

        private void OnOobRowKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode is not (KeyCode.Return or KeyCode.KeypadEnter or KeyCode.Space))
            {
                return;
            }

            if (evt.currentTarget is Label { userData: string unitId } && bridgeHost != null)
            {
                if (_oobList != null)
                {
                    for (var i = 0; i < _panelState.UnitRows.Count; i++)
                    {
                        if (_panelState.UnitRows[i].UnitId == unitId)
                        {
                            _oobList.SetSelection(i);
                            break;
                        }
                    }
                }

                bridgeHost.SelectUnit(unitId);
                evt.StopPropagation();
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
                _oobList.itemsSource = _panelState.UnitRows.ToList();
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
