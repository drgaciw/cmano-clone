// PE-UX-W4: Unified Platform Editor shell (P-PE-04) — Catalog | Import tabs + shared status/health.
// Coordinates existing panel hosts; preserves import staging state across tab switches (hosts stay enabled).
#if UNITY_5_3_OR_NEWER
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class PlatformEditorShellHost : MonoBehaviour
    {
        private const string RootName = "platform-editor-shell-root";
        private const string ModeTitleName = "platform-editor-shell-mode-title";
        private const string StatusName = "platform-editor-shell-status";
        private const string HealthName = "platform-editor-shell-health";
        private const string TabCatalogName = "platform-editor-shell-tab-catalog";
        private const string TabImportName = "platform-editor-shell-tab-import";
        private const string TabsName = "platform-editor-shell-tabs";
        private const string CatalogSlotName = "platform-editor-shell-catalog-slot";
        private const string ImportSlotName = "platform-editor-shell-import-slot";
        private const string SlotHiddenClass = "platform-editor-shell-slot--hidden";
        private const string BrowseRootClass = "platform-editor-shell--browse";
        private const string ImportRootClass = "platform-editor-shell--import";

        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;
        [SerializeField] private PlatformCatalogViewerHost? catalogHost;
        [SerializeField] private PlatformImportPanelHost? importHost;
        [SerializeField] private C2AccessibilityScalePercent accessibilityScale = C2AccessibilityScalePercent.OneHundred;
        [SerializeField] private bool reducedMotion = true;

        private UIDocument _document = null!;
        private Label? _modeTitle;
        private Label? _status;
        private Label? _health;
        private Button? _tabCatalog;
        private Button? _tabImport;
        private VisualElement? _catalogSlot;
        private VisualElement? _importSlot;
        private VisualElement? _tabs;
        private PlatformEditorShellState _state = PlatformEditorShellProjection.Bind();
        private bool _wired;

        /// <summary>Current shell projection state (test / curator sync).</summary>
        public PlatformEditorShellState State => _state;

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
            ApplyState(_state);
            ApplyAccessibility();
            ApplyPanelVisibility();
        }

        /// <summary>Apply shell mode without destroying child host staging state.</summary>
        public void SetMode(PlatformEditorShellMode mode)
        {
            _state = PlatformEditorShellProjection.WithMode(_state, mode);
            ApplyState(_state);
        }

        public void SetSelectedPlatformId(string? platformId)
        {
            _state = PlatformEditorShellProjection.WithSelection(_state, platformId);
            catalogHost?.SelectPlatform(platformId);
            ApplyState(_state);
        }

        public void SetStatusSummary(string statusSummary)
        {
            _state = PlatformEditorShellProjection.WithStatus(_state, statusSummary);
            ApplyState(_state);
        }

        public void SetHealthSummary(string healthLine)
        {
            if (_health != null)
            {
                _health.text = healthLine;
            }
        }

        public void RefreshHealthFromHosts()
        {
            var blocked = importHost?.BlockedFindingCount ?? 0;
            var pending = importHost?.PendingDiffCount ?? 0;
            var edges = catalogHost?.DependencyEdgeCount ?? 0;
            SetHealthSummary(PlatformCatalogHealthProjection.Format(blocked, pending, edges));
        }

        private void TryWireElements()
        {
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            _modeTitle ??= root.Q<Label>(ModeTitleName);
            _status ??= root.Q<Label>(StatusName);
            _health ??= root.Q<Label>(HealthName);
            _catalogSlot ??= root.Q(CatalogSlotName);
            _importSlot ??= root.Q(ImportSlotName);

            if (_tabCatalog == null)
            {
                _tabCatalog = root.Q<Button>(TabCatalogName);
                if (_tabCatalog != null)
                {
                    _tabCatalog.clicked += () => SetMode(PlatformEditorShellMode.Catalog);
                }
            }

            if (_tabImport == null)
            {
                _tabImport = root.Q<Button>(TabImportName);
                if (_tabImport != null)
                {
                    _tabImport.clicked += () => SetMode(PlatformEditorShellMode.Import);
                }
            }

            if (_tabs == null)
            {
                _tabs = root.Q(TabsName);
                if (_tabs != null)
                {
                    _tabs.RegisterCallback<KeyDownEvent>(OnTabsKeyDown);
                }
            }

            if (catalogHost != null)
            {
                catalogHost.CommsLinkNavigateRequested -= OnCommsLinkNavigate;
                catalogHost.CommsLinkNavigateRequested += OnCommsLinkNavigate;
            }

            _wired = root.Q(RootName) != null;
        }

        private void OnCommsLinkNavigate(string? platformId)
        {
            SetSelectedPlatformId(platformId);
            catalogHost?.NavigateToLinksSection(platformId);
            SetStatusSummary(
                string.IsNullOrWhiteSpace(platformId)
                    ? "Navigated to Links"
                    : $"Navigated to Links · {platformId}");
        }

        private void OnTabsKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode is KeyCode.RightArrow or KeyCode.LeftArrow
                || (evt.keyCode == KeyCode.Tab && evt.ctrlKey))
            {
                _state = PlatformEditorShellProjection.CycleMode(_state);
                ApplyState(_state);
                evt.StopPropagation();
            }
        }

        private void ApplyState(PlatformEditorShellState state)
        {
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            root.EnableInClassList(BrowseRootClass, state.Mode == PlatformEditorShellMode.Catalog);
            root.EnableInClassList(ImportRootClass, state.Mode == PlatformEditorShellMode.Import);

            if (_modeTitle != null)
            {
                _modeTitle.text = state.ModeTitle;
            }

            if (_status != null)
            {
                _status.text = state.StatusSummary;
            }

            _tabCatalog?.EnableInClassList("platform-editor-shell-tab--active", state.CatalogTabActive);
            _tabImport?.EnableInClassList("platform-editor-shell-tab--active", state.ImportTabActive);

            _catalogSlot?.EnableInClassList(SlotHiddenClass, !state.CatalogContentVisible);
            _importSlot?.EnableInClassList(SlotHiddenClass, !state.ImportContentVisible);

            // Keep both hosts alive so Import staging survives Catalog tab visits.
            catalogHost?.SetShellVisible(state.CatalogContentVisible);
            importHost?.SetShellVisible(state.ImportContentVisible);

            RefreshHealthFromHosts();
        }

        private void ApplyAccessibility()
        {
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            var settings = new C2AccessibilitySettings(accessibilityScale, reducedMotion);
            root.EnableInClassList("aegis-scale-100", settings.ScalePercent == C2AccessibilityScalePercent.OneHundred);
            root.EnableInClassList("aegis-scale-125", settings.ScalePercent == C2AccessibilityScalePercent.OneTwentyFive);
            root.EnableInClassList("aegis-scale-150", settings.ScalePercent == C2AccessibilityScalePercent.OneFifty);
            root.EnableInClassList("reduced-motion", settings.ReducedMotion);
        }

        private void ApplyPanelVisibility()
        {
            var root = _document.rootVisualElement;
            if (root != null)
            {
                root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
#endif
