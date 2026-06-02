// Doc-20 right unit detail panel — UI Toolkit bound to DelegationBridgeHost.
#if UNITY_5_3_OR_NEWER
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class RightUnitPanelHost : MonoBehaviour
    {
        private const string RootName = "unit-detail-root";
        private const string UnitIdName = "unit-id-line";
        private const string StatusName = "status-line";
        private const string MagazineName = "magazine-line";
        private const string EmconName = "emcon-line";
        private const string DoctrineName = "doctrine-line";
        private const string ContactName = "contact-line";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _unitIdLine;
        private Label? _statusLine;
        private Label? _magazineLine;
        private Label? _emconLine;
        private Label? _doctrineLine;
        private Label? _contactLine;
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
            _unitIdLine = panel.Q<Label>(UnitIdName);
            _statusLine = panel.Q<Label>(StatusName);
            _magazineLine = panel.Q<Label>(MagazineName);
            _emconLine = panel.Q<Label>(EmconName);
            _doctrineLine = panel.Q<Label>(DoctrineName);
            _contactLine = panel.Q<Label>(ContactName);
            _wired = _unitIdLine != null && _statusLine != null && _magazineLine != null &&
                     _emconLine != null && _doctrineLine != null;

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }
        }

        private void Refresh()
        {
            if (!_wired || bridgeHost == null)
            {
                return;
            }

            var state = UnitDetailPanelBinder.Bind(
                bridgeHost.LastUnitDetail,
                bridgeHost.Presentation.ResolveContactLine());
            _unitIdLine!.text = state.UnitIdLine;
            _statusLine!.text = state.StatusLine;
            _magazineLine!.text = state.MagazineLine;
            _emconLine!.text = state.EmconLine;
            _doctrineLine!.text = state.DoctrineLine;
            if (_contactLine != null)
            {
                _contactLine.text = state.ContactLine;
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