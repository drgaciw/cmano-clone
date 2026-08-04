// CMD-27 scenario library browse + pre-load feasibility — UI Toolkit host.
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class ScenarioLibraryPanelHost : MonoBehaviour
    {
        private const string RootName = "scenario-library-root";
        private const string ListName = "scenario-library-list";
        private const string ZeroStateName = "preview-zero-state";
        private const string PreviewFieldsName = "preview-fields";

        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;
        [SerializeField] private bool reloadOnEnable = true;

        private UIDocument _document = null!;
        private ListView? _list;
        private Label? _zeroState;
        private VisualElement? _previewFields;
        private Label? _previewTitle;
        private Label? _previewId;
        private Label? _previewAvailability;
        private Label? _previewPolicy;
        private Label? _previewTl;
        private Label? _previewSeed;
        private Label? _previewLocationYear;
        private Label? _previewProvenance;
        private Label? _previewDifficulty;
        private Label? _previewComplexity;
        private Label? _previewSource;

        private IReadOnlyList<ScenarioLibraryEntry> _entries = System.Array.Empty<ScenarioLibraryEntry>();
        private ScenarioLibraryPresentation _presentation = ScenarioLibraryPresentation.Empty;
        private ScenarioLibraryPreviewPresentation _preview = ScenarioLibraryPreviewPresentation.ZeroState;
        private bool _wired;
        private int _selectedIndex = -1;

        /// <summary>Last listed entries (tests / debug).</summary>
        public IReadOnlyList<ScenarioLibraryEntry> LastEntries => _entries;

        /// <summary>Last list presentation.</summary>
        public ScenarioLibraryPresentation LastPresentation => _presentation;

        /// <summary>Last preview presentation.</summary>
        public ScenarioLibraryPreviewPresentation LastPreview => _preview;

        private void Reset()
        {
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
            if (reloadOnEnable)
            {
                ReloadFromDisk();
            }

            Refresh();
        }

        private void LateUpdate()
        {
            if (!showPanel)
            {
                return;
            }

            if (!_wired)
            {
                TryWireElements();
                Refresh();
            }

            var root = _document.rootVisualElement?.Q(RootName);
            if (root != null)
            {
                root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>Reload library rows from <see cref="ScenarioDataPaths"/> (or empty if unresolved).</summary>
        public void ReloadFromDisk(ICatalogReader? catalog = null)
        {
            var dir = ScenarioDataPaths.TryResolveScenariosDirectory();
            if (dir == null)
            {
                _entries = System.Array.Empty<ScenarioLibraryEntry>();
            }
            else
            {
                _entries = ScenarioLibraryLister.ListFromDirectory(dir, catalog);
            }

            _selectedIndex = -1;
            ApplyEntries(_entries);
        }

        /// <summary>Apply an entry list (tests / host wiring without disk).</summary>
        public void ApplyEntries(IReadOnlyList<ScenarioLibraryEntry> entries)
        {
            _entries = entries ?? System.Array.Empty<ScenarioLibraryEntry>();
            _presentation = ScenarioLibraryApplyState.Apply(_entries);
            if (_selectedIndex < 0 || _selectedIndex >= _entries.Count)
            {
                _selectedIndex = -1;
                _preview = ScenarioLibraryApplyState.ApplyPreview(null);
            }
            else
            {
                _preview = ScenarioLibraryApplyState.ApplyPreview(_entries[_selectedIndex]);
            }

            BindList();
            BindPreview();
        }

        /// <summary>Select row by index and refresh preview.</summary>
        public void SelectIndex(int index)
        {
            if (index < 0 || index >= _entries.Count)
            {
                _selectedIndex = -1;
                _preview = ScenarioLibraryApplyState.ApplyPreview(null);
            }
            else
            {
                _selectedIndex = index;
                _preview = ScenarioLibraryApplyState.ApplyPreview(_entries[index]);
            }

            if (_list != null)
            {
                _list.selectedIndex = _selectedIndex;
            }

            BindPreview();
        }

        private void TryWireElements()
        {
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            var panel = root.Q<VisualElement>(RootName) ?? root;
            _list = panel.Q<ListView>(ListName);
            _zeroState = panel.Q<Label>(ZeroStateName);
            _previewFields = panel.Q<VisualElement>(PreviewFieldsName);
            _previewTitle = panel.Q<Label>("preview-title");
            _previewId = panel.Q<Label>("preview-id");
            _previewAvailability = panel.Q<Label>("preview-availability");
            _previewPolicy = panel.Q<Label>("preview-policy");
            _previewTl = panel.Q<Label>("preview-tl");
            _previewSeed = panel.Q<Label>("preview-seed");
            _previewLocationYear = panel.Q<Label>("preview-location-year");
            _previewProvenance = panel.Q<Label>("preview-provenance");
            _previewDifficulty = panel.Q<Label>("preview-difficulty");
            _previewComplexity = panel.Q<Label>("preview-complexity");
            _previewSource = panel.Q<Label>("preview-source");

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
                _list.selectionType = SelectionType.Single;
                _list.selectionChanged -= OnSelectionChanged;
                _list.selectionChanged += OnSelectionChanged;
            }

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }

            _wired = _list != null;
        }

        private void OnSelectionChanged(IEnumerable<object> _)
        {
            if (_list == null)
            {
                return;
            }

            SelectIndex(_list.selectedIndex);
        }

        private void Refresh()
        {
            if (!_wired)
            {
                return;
            }

            BindList();
            BindPreview();

            var root = _document.rootVisualElement?.Q(RootName);
            if (root != null)
            {
                root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void BindList()
        {
            if (_list == null)
            {
                return;
            }

            _list.itemsSource = _presentation.Lines as System.Collections.IList
                ?? new List<string>(_presentation.Lines);
            _list.Rebuild();
            _list.selectedIndex = _selectedIndex;
        }

        private void BindPreview()
        {
            var zero = _preview.IsZeroState;
            if (_zeroState != null)
            {
                _zeroState.text = zero
                    ? ScenarioLibraryApplyState.ZeroStateInstruction
                    : string.Empty;
                _zeroState.style.display = zero ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_previewFields != null)
            {
                _previewFields.style.display = zero ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (zero)
            {
                return;
            }

            SetLabel(_previewTitle, _preview.TitleLine);
            SetLabel(_previewId, _preview.ScenarioIdLine);
            SetLabel(_previewAvailability, _preview.AvailabilityLine);
            SetLabel(_previewPolicy, _preview.PolicyLine);
            SetLabel(_previewTl, _preview.TlBranchLine);
            SetLabel(_previewSeed, _preview.SeedLine);
            SetLabel(_previewLocationYear, _preview.LocationYearLine);
            SetLabel(_previewProvenance, _preview.ProvenanceLine);
            SetLabel(_previewDifficulty, _preview.DifficultyLine);
            SetLabel(_previewComplexity, _preview.ComplexityLine);
            SetLabel(_previewSource, _preview.SourcePathLine);
        }

        private static void SetLabel(Label? label, string text)
        {
            if (label != null)
            {
                label.text = text ?? string.Empty;
            }
        }
    }
}
#endif
