// CMD-24 magazine loadout depth — weapon remaining/capacity + armable airframe feasibility.
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MagazineLoadoutPanelHost : MonoBehaviour
    {
        private const string RootName = "magazine-loadout-root";
        private const string HeaderName = "magazine-loadout-header";
        private const string EmptyName = "magazine-loadout-empty";
        private const string ListName = "magazine-loadout-list";
        private const string FeasibilityName = "magazine-loadout-feasibility";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;
        [SerializeField] private int roundsPerAirframe = 6;

        private UIDocument _document = null!;
        private Label? _headerLine;
        private Label? _emptyLine;
        private Label? _feasibilityLine;
        private ListView? _list;
        private MagazineLoadoutPresentation _presentation = MagazineLoadoutPresentation.Empty;
        private bool _wired;

        /// <summary>Last applied magazine presentation (CMD-24).</summary>
        public MagazineLoadoutPresentation LastPresentation => _presentation;

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

            bridgeHost.RefreshMagazineLoadout();
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
            _headerLine = panel.Q<Label>(HeaderName);
            _emptyLine = panel.Q<Label>(EmptyName);
            _feasibilityLine = panel.Q<Label>(FeasibilityName);
            _list = panel.Q<ListView>(ListName);

            if (_list != null)
            {
                _list.makeItem = () => new Label();
                _list.bindItem = (element, index) =>
                {
                    if (element is Label label && index >= 0 && index < _presentation.Lines.Count)
                    {
                        label.text = _presentation.Lines[index];
                    }
                };
                _list.selectionType = SelectionType.None;
            }

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }

            _wired = _headerLine != null || _list != null;
        }

        /// <summary>Apply presentation via headless apply-state (tests / direct bind).</summary>
        public void ApplyPresentation(MagazineLoadoutPresentation presentation)
        {
            _presentation = presentation ?? MagazineLoadoutPresentation.Empty;
            ApplyPresentationToUi();
        }

        private void Refresh()
        {
            if (!_wired || bridgeHost == null)
            {
                return;
            }

            _presentation = MagazineLoadoutApplyState.Apply(
                bridgeHost.LastMagazineLoadout,
                bridgeHost.HasMagazineLoadoutData);

            ApplyPresentationToUi();

            var root = _document.rootVisualElement?.Q(RootName);
            if (root != null)
            {
                root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void ApplyPresentationToUi()
        {
            if (_headerLine != null)
            {
                _headerLine.text = _presentation.HeaderLine;
            }

            if (_emptyLine != null)
            {
                _emptyLine.text = _presentation.EmptyStateLine ?? string.Empty;
                _emptyLine.style.display = string.IsNullOrEmpty(_presentation.EmptyStateLine)
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;
            }

            if (_list != null)
            {
                _list.itemsSource = _presentation.Lines as System.Collections.IList
                    ?? new List<string>(_presentation.Lines);
                _list.Rebuild();
            }

            if (_feasibilityLine != null)
            {
                if (!_presentation.HasMagazineData || _presentation.Rows.Count == 0)
                {
                    _feasibilityLine.text = string.Empty;
                }
                else
                {
                    var stock = _presentation.Aggregate.TotalRemaining;
                    _feasibilityLine.text = MagazineLoadoutApplyState.FormatFeasibilityLine(
                        stock,
                        roundsPerAirframe);
                }
            }
        }
    }
}
#endif
