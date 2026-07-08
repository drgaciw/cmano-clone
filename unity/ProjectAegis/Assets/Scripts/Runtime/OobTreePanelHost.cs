// OOB left-drawer slice — UI Toolkit panel bound to DelegationBridgeHost.
#if UNITY_5_3_OR_NEWER
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
        private const string OobListName = "oob-list";

        /// <summary>Estimated single-line row height (px) at 100% scale — used only to seed
        /// fixed-height virtualization; not a pixel-perfect layout guarantee (req 20 AC-10).</summary>
        private const float FixedRowHeightPx = 18f;

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        /// <summary>req 20 AC-1 / F5 — text scale tier (accessibility-requirements.md §3). Default
        /// 100% until a settings UI ships.</summary>
        [SerializeField] private C2TextScalePercent scalePercent = C2AccessibilitySettings.DefaultScalePercent;

        private UIDocument _document = null!;
        private ListView? _oobList;
        private OobTreePanelState _panelState = new(Array.Empty<OobTreeDisplayRow>());
        private readonly PanelRefreshGate<OobTreeDisplayRow> _refreshGate = new();
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

                // req 20 AC-10 / NFR virtualization — explicit fixed-height virtualization so
                // Unity only realizes viewport (+ pool) elements at 5k+ rows instead of the whole
                // list (see also the itemsSource/Rebuild dirty-gate in Refresh()).
                _oobList.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
                _oobList.fixedItemHeight = FixedRowHeightPx;
                _wired = true;
            }

            ApplyAccessibilityScale(root.Q<VisualElement>(RootName) ?? root);
        }

        /// <summary>req 20 AC-1 / F5 — toggles the AegisTokens.uss scale-tier class matching
        /// <see cref="scalePercent"/> onto the panel root; font-size is inherited from there down
        /// to every Label under ".oob-tree-list" (accessibility-requirements.md §3).</summary>
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
            if (bridgeHost == null)
            {
                return;
            }

            var candidate = OobTreePanelBinder.Bind(bridgeHost.LastOobTree, bridgeHost.SelectedUnitId);

            // req 20 AC-10 — skip itemsSource reassignment + Rebuild() when the bound snapshot is
            // structurally unchanged from the last frame that was actually pushed to the
            // ListView. Rebuild() tears down pooled/visible elements; doing that unconditionally
            // every LateUpdate defeats ListView's recycling virtualization at scale.
            if (_oobList != null && _refreshGate.IsDirty(candidate.UnitRows))
            {
                _panelState = candidate;
                _oobList.itemsSource = _panelState.UnitRows.ToList();
                _oobList.Rebuild();
                _refreshGate.MarkApplied(_panelState.UnitRows);
            }
            else if (_oobList == null)
            {
                // No ListView wired yet — keep panelState current so the next successful wire
                // picks up the latest data immediately.
                _panelState = candidate;
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