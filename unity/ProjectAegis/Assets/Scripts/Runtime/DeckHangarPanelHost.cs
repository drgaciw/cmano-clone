// CMD-24 deck/hangar capacity — spot totals/occupied/ready bands (no deck-cycle timers).
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class DeckHangarPanelHost : MonoBehaviour
    {
        private const string RootName = "deck-hangar-root";
        private const string HeaderName = "deck-hangar-header";
        private const string EmptyName = "deck-hangar-empty";
        private const string ListName = "deck-hangar-list";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _headerLine;
        private Label? _emptyLine;
        private ListView? _list;
        private DeckHangarCapacityPresentation _presentation = DeckHangarCapacityPresentation.Empty;
        private bool _wired;

        /// <summary>Last applied deck/hangar presentation (CMD-24).</summary>
        public DeckHangarCapacityPresentation LastPresentation => _presentation;

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

            bridgeHost.RefreshDeckHangar();
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
        public void ApplyPresentation(DeckHangarCapacityPresentation presentation)
        {
            _presentation = presentation ?? DeckHangarCapacityPresentation.Empty;
            ApplyPresentationToUi();
        }

        private void Refresh()
        {
            if (!_wired || bridgeHost == null)
            {
                return;
            }

            _presentation = DeckHangarCapacityApplyState.Apply(
                bridgeHost.LastDeckHangar,
                bridgeHost.HasDeckHangarData);

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
        }
    }
}
#endif
