// DRG-67: Ground Ops panel — brigade+ formation list (CMD-26 Phase A).
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class GroundOpsPanelHost : MonoBehaviour
    {
        private const string RootName = "ground-ops-root";
        private const string HeaderName = "ground-ops-header";
        private const string EmptyName = "ground-ops-empty";
        private const string ListName = "ground-ops-list";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _headerLine;
        private Label? _emptyLine;
        private ListView? _formationList;
        private IReadOnlyList<GroundFormationEntry> _entries = System.Array.Empty<GroundFormationEntry>();
        private bool _wired;

        /// <summary>Last projected ground formation entries (DRG-67).</summary>
        public IReadOnlyList<GroundFormationEntry> Entries => _entries;

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
            _headerLine = panel.Q<Label>(HeaderName);
            _emptyLine = panel.Q<Label>(EmptyName);
            _formationList = panel.Q<ListView>(ListName);

            if (_formationList != null)
            {
                _formationList.makeItem = () => new Label();
                _formationList.bindItem = (element, index) =>
                {
                    if (element is Label label && index >= 0 && index < _entries.Count)
                    {
                        label.text = $"{_entries[index].DisplayName}  {_entries[index].StrengthBand}";
                    }
                };
                _formationList.selectionType = SelectionType.None;
            }

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }

            _wired = _headerLine != null || _formationList != null;
        }

        private void Refresh()
        {
            if (!_wired)
            {
                return;
            }

            var agg = GroundOpsProjection.Aggregate(_entries);

            if (_headerLine != null)
            {
                _headerLine.text = agg.SummaryLine;
            }

            if (_emptyLine != null)
            {
                _emptyLine.text = _entries.Count == 0 ? "No ground formations tracked." : string.Empty;
                _emptyLine.style.display = _entries.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_formationList != null)
            {
                _formationList.itemsSource = (System.Collections.IList)_entries;
                _formationList.Rebuild();
            }

            var rootEl = _document.rootVisualElement?.Q(RootName);
            if (rootEl != null)
            {
                rootEl.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>Apply formation entries directly (headless / test path).</summary>
        public void Apply(IReadOnlyList<GroundFormationEntry> entries)
        {
            _entries = entries ?? System.Array.Empty<GroundFormationEntry>();
            if (_wired)
            {
                Refresh();
            }
        }
    }
}
#endif
