// CMD-27 scenario library browse + CMD-27.12 campaign artifact class — UI Toolkit host.
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario;
using ProjectAegis.Data.Scenario.Campaign;
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
        private const string CampaignListName = "campaign-library-list";
        private const string ZeroStateName = "preview-zero-state";
        private const string CampaignZeroStateName = "campaign-preview-zero-state";
        private const string PreviewFieldsName = "preview-fields";
        private const string CampaignPreviewFieldsName = "campaign-preview-fields";

        private enum PreviewMode
        {
            ScenarioZero,
            ScenarioSelected,
            CampaignZero,
            CampaignSelected,
        }

        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;
        [SerializeField] private bool reloadOnEnable = true;

        private UIDocument _document = null!;
        private ListView? _list;
        private ListView? _campaignList;
        private Label? _zeroState;
        private Label? _campaignZeroState;
        private VisualElement? _previewFields;
        private VisualElement? _campaignPreviewFields;
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
        private Label? _campaignPreviewTitle;
        private Label? _campaignPreviewId;
        private Label? _campaignPreviewAvailability;
        private Label? _campaignPreviewProgress;
        private Label? _campaignPreviewNext;
        private Label? _campaignPreviewSource;

        private IReadOnlyList<ScenarioLibraryEntry> _entries = System.Array.Empty<ScenarioLibraryEntry>();
        private IReadOnlyList<CampaignLibraryEntry> _campaignEntries = System.Array.Empty<CampaignLibraryEntry>();
        private ScenarioLibraryPresentation _presentation = ScenarioLibraryPresentation.Empty;
        private CampaignLibraryPresentation _campaignPresentation = CampaignLibraryPresentation.Empty;
        private ScenarioLibraryPreviewPresentation _preview = ScenarioLibraryPreviewPresentation.ZeroState;
        private CampaignLibraryPreviewPresentation _campaignPreview = CampaignLibraryPreviewPresentation.ZeroState;
        private PreviewMode _previewMode = PreviewMode.ScenarioZero;
        private bool _wired;
        private int _selectedIndex = -1;
        private int _selectedCampaignIndex = -1;

        /// <summary>Last listed scenario entries (tests / debug).</summary>
        public IReadOnlyList<ScenarioLibraryEntry> LastEntries => _entries;

        /// <summary>Last listed campaign entries (CMD-27.12).</summary>
        public IReadOnlyList<CampaignLibraryEntry> LastCampaignEntries => _campaignEntries;

        /// <summary>Last list presentation.</summary>
        public ScenarioLibraryPresentation LastPresentation => _presentation;

        /// <summary>Last campaign list presentation.</summary>
        public CampaignLibraryPresentation LastCampaignPresentation => _campaignPresentation;

        /// <summary>Last preview presentation.</summary>
        public ScenarioLibraryPreviewPresentation LastPreview => _preview;

        /// <summary>Last campaign preview presentation.</summary>
        public CampaignLibraryPreviewPresentation LastCampaignPreview => _campaignPreview;

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

            var campaignsDir = ScenarioDataPaths.TryResolveCampaignsDirectory();
            if (campaignsDir == null)
            {
                _campaignEntries = System.Array.Empty<CampaignLibraryEntry>();
            }
            else
            {
                _campaignEntries = CampaignLibraryLister.ListFromDirectory(campaignsDir, dir);
            }

            _selectedIndex = -1;
            _selectedCampaignIndex = -1;
            ApplyEntries(_entries);
            ApplyCampaignEntries(_campaignEntries);
        }

        /// <summary>Apply a scenario entry list (tests / host wiring without disk).</summary>
        public void ApplyEntries(IReadOnlyList<ScenarioLibraryEntry> entries)
        {
            _entries = entries ?? System.Array.Empty<ScenarioLibraryEntry>();
            _presentation = ScenarioLibraryApplyState.Apply(_entries);
            if (_selectedIndex < 0 || _selectedIndex >= _entries.Count)
            {
                _selectedIndex = -1;
                if (_previewMode is PreviewMode.ScenarioSelected or PreviewMode.ScenarioZero)
                {
                    _preview = ScenarioLibraryApplyState.ApplyPreview(null);
                    _previewMode = PreviewMode.ScenarioZero;
                }
            }
            else
            {
                _preview = ScenarioLibraryApplyState.ApplyPreview(_entries[_selectedIndex]);
                _previewMode = PreviewMode.ScenarioSelected;
            }

            BindList();
            BindPreview();
        }

        /// <summary>Apply a campaign entry list (CMD-27.12; separate from flat scenarios).</summary>
        public void ApplyCampaignEntries(IReadOnlyList<CampaignLibraryEntry> entries)
        {
            _campaignEntries = entries ?? System.Array.Empty<CampaignLibraryEntry>();
            _campaignPresentation = CampaignLibraryApplyState.Apply(_campaignEntries);
            if (_selectedCampaignIndex < 0 || _selectedCampaignIndex >= _campaignEntries.Count)
            {
                _selectedCampaignIndex = -1;
                if (_previewMode is PreviewMode.CampaignSelected or PreviewMode.CampaignZero)
                {
                    _campaignPreview = CampaignLibraryApplyState.ApplyPreview(null);
                    _previewMode = PreviewMode.CampaignZero;
                }
            }
            else
            {
                _campaignPreview = CampaignLibraryApplyState.ApplyPreview(_campaignEntries[_selectedCampaignIndex]);
                _previewMode = PreviewMode.CampaignSelected;
            }

            BindCampaignList();
            BindPreview();
        }

        /// <summary>Select scenario row by index and refresh preview.</summary>
        public void SelectIndex(int index)
        {
            _selectedCampaignIndex = -1;
            if (_campaignList != null)
            {
                _campaignList.selectedIndex = -1;
            }

            if (index < 0 || index >= _entries.Count)
            {
                _selectedIndex = -1;
                _preview = ScenarioLibraryApplyState.ApplyPreview(null);
                _previewMode = PreviewMode.ScenarioZero;
            }
            else
            {
                _selectedIndex = index;
                _preview = ScenarioLibraryApplyState.ApplyPreview(_entries[index]);
                _previewMode = PreviewMode.ScenarioSelected;
            }

            if (_list != null)
            {
                _list.selectedIndex = _selectedIndex;
            }

            BindPreview();
        }

        /// <summary>Select campaign row by index and refresh campaign preview (CMD-27.12).</summary>
        public void SelectCampaignIndex(int index)
        {
            _selectedIndex = -1;
            if (_list != null)
            {
                _list.selectedIndex = -1;
            }

            if (index < 0 || index >= _campaignEntries.Count)
            {
                _selectedCampaignIndex = -1;
                _campaignPreview = CampaignLibraryApplyState.ApplyPreview(null);
                _previewMode = PreviewMode.CampaignZero;
            }
            else
            {
                _selectedCampaignIndex = index;
                _campaignPreview = CampaignLibraryApplyState.ApplyPreview(_campaignEntries[index]);
                _previewMode = PreviewMode.CampaignSelected;
            }

            if (_campaignList != null)
            {
                _campaignList.selectedIndex = _selectedCampaignIndex;
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
            _campaignList = panel.Q<ListView>(CampaignListName);
            _zeroState = panel.Q<Label>(ZeroStateName);
            _campaignZeroState = panel.Q<Label>(CampaignZeroStateName);
            _previewFields = panel.Q<VisualElement>(PreviewFieldsName);
            _campaignPreviewFields = panel.Q<VisualElement>(CampaignPreviewFieldsName);
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
            _campaignPreviewTitle = panel.Q<Label>("campaign-preview-title");
            _campaignPreviewId = panel.Q<Label>("campaign-preview-id");
            _campaignPreviewAvailability = panel.Q<Label>("campaign-preview-availability");
            _campaignPreviewProgress = panel.Q<Label>("campaign-preview-progress");
            _campaignPreviewNext = panel.Q<Label>("campaign-preview-next");
            _campaignPreviewSource = panel.Q<Label>("campaign-preview-source");

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

            if (_campaignList != null)
            {
                _campaignList.makeItem = () => new Label();
                _campaignList.bindItem = (element, index) =>
                {
                    if (element is Label label && index >= 0 && index < _campaignPresentation.Lines.Count)
                    {
                        label.text = _campaignPresentation.Lines[index];
                    }
                };
                _campaignList.selectionType = SelectionType.Single;
                _campaignList.selectionChanged -= OnCampaignSelectionChanged;
                _campaignList.selectionChanged += OnCampaignSelectionChanged;
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

        private void OnCampaignSelectionChanged(IEnumerable<object> _)
        {
            if (_campaignList == null)
            {
                return;
            }

            SelectCampaignIndex(_campaignList.selectedIndex);
        }

        private void Refresh()
        {
            if (!_wired)
            {
                return;
            }

            BindList();
            BindCampaignList();
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

        private void BindCampaignList()
        {
            if (_campaignList == null)
            {
                return;
            }

            _campaignList.itemsSource = _campaignPresentation.Lines as System.Collections.IList
                ?? new List<string>(_campaignPresentation.Lines);
            _campaignList.Rebuild();
            _campaignList.selectedIndex = _selectedCampaignIndex;
        }

        private void BindPreview()
        {
            var showScenarioFields = _previewMode == PreviewMode.ScenarioSelected;
            var showCampaignFields = _previewMode == PreviewMode.CampaignSelected;
            var showScenarioZero = _previewMode == PreviewMode.ScenarioZero;
            var showCampaignZero = _previewMode == PreviewMode.CampaignZero;

            // Default zero-state when neither list has a selection: scenario instruction,
            // unless the user last focused campaigns (CampaignZero).
            if (_previewMode == PreviewMode.ScenarioZero && _selectedCampaignIndex < 0 && _selectedIndex < 0)
            {
                showScenarioZero = true;
                showCampaignZero = false;
            }

            if (_zeroState != null)
            {
                _zeroState.text = showScenarioZero
                    ? ScenarioLibraryApplyState.ZeroStateInstruction
                    : string.Empty;
                _zeroState.style.display = showScenarioZero ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_campaignZeroState != null)
            {
                _campaignZeroState.text = showCampaignZero
                    ? CampaignLibraryApplyState.ZeroStateInstruction
                    : string.Empty;
                _campaignZeroState.style.display = showCampaignZero ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_previewFields != null)
            {
                _previewFields.style.display = showScenarioFields ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_campaignPreviewFields != null)
            {
                _campaignPreviewFields.style.display = showCampaignFields ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (showScenarioFields)
            {
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

            if (showCampaignFields)
            {
                SetLabel(_campaignPreviewTitle, _campaignPreview.TitleLine);
                SetLabel(_campaignPreviewId, _campaignPreview.CampaignIdLine);
                SetLabel(_campaignPreviewAvailability, _campaignPreview.AvailabilityLine);
                SetLabel(_campaignPreviewProgress, _campaignPreview.ProgressLine);
                SetLabel(_campaignPreviewNext, _campaignPreview.NextScenarioLine);
                SetLabel(_campaignPreviewSource, _campaignPreview.SourcePathLine);
            }
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
