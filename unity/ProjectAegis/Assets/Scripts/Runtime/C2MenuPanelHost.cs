# CMD-28 C2 menu / basemap layer checklist host — thin UI Toolkit over C2MenuProjection.
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    /// <summary>
    /// Thin host for the CMD-28 C2 menu catalogue.
    /// Binds <see cref="C2MenuProjection.ProjectDefault"/> with the map host layer stack when present.
    /// Layer rows call <see cref="MapPlaceholderPanelHost.ToggleLayer"/> (UI-local; not DecisionLog).
    /// Next/Previous Unit rows cycle the C2 unit selection via <see cref="DelegationBridgeHost.SelectUnit"/>
    /// (also UI-local; ADR-010 exempt per CMD-28.3). Zoom/2D-3D-toggle/Measure remain enabled per the
    /// C2MenuProjection contract but have no backing subsystem yet (no camera/measure state exists) —
    /// intentionally left unwired here pending that subsystem work.
    /// Shortcut labels are shown inline for discovery (CMD-28.1).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class C2MenuPanelHost : MonoBehaviour
    {
        private const string RootName = "c2-menu-root";
        private const string ListName = "c2-menu-list";
        private const string SummaryName = "c2-menu-summary";
        private const string StatusName = "c2-menu-status";
        private const string LayerIdPrefix = "layer-";
        private const string NextUnitId = "view-next-unit";
        private const string PrevUnitId = "view-prev-unit";

        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;
        [SerializeField] private MapPlaceholderPanelHost? mapHost;
        [SerializeField] private DelegationBridgeHost? bridgeHost;

        private UIDocument _document = null!;
        private VisualElement? _menuList;
        private Label? _summaryLine;
        private Label? _statusLine;
        private IReadOnlyList<C2MenuItemEntry> _items = Array.Empty<C2MenuItemEntry>();
        private int _bookmarkCount;
        private bool _wired;
        private int _lastLayerVisible = -1;
        private int _lastLayerTotal = -1;
        private int _lastBookmarkCount = -1;
        private bool _refreshedOnce;

        /// <summary>Last projected menu items (tests / sign-off).</summary>
        public IReadOnlyList<C2MenuItemEntry> LastItems => _items;

        /// <summary>Last layer summary label (mirrors map HUD when map host present).</summary>
        public string LastSummaryLabel { get; private set; } = "LAYERS: —";

        /// <summary>Last status note shown under the list.</summary>
        public string? LastStatus { get; private set; }

        /// <summary>Resolved map host (Inspector, Find, or null).</summary>
        public MapPlaceholderPanelHost? MapHost => mapHost;

        /// <summary>Bookmark count fed into <see cref="C2MenuProjection"/> (UI-local).</summary>
        public int BookmarkCount
        {
            get => _bookmarkCount;
            set => _bookmarkCount = Math.Max(0, value);
        }

        private void Reset()
        {
            _document = GetComponent<UIDocument>();
            UiDocumentPanelSettingsBootstrap.EnsureDocument(_document);
            if (mapHost == null)
            {
                mapHost = FindFirstObjectByType<MapPlaceholderPanelHost>();
            }
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

            ResolveMapHost();
        }

        private void OnEnable()
        {
            ResolveMapHost();
            TryWireElements();
            Refresh(force: true);
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
            }

            ResolveMapHost();
            Refresh(force: false);
        }

        /// <summary>Force rebuild from current layer stack / bookmarks.</summary>
        public void Refresh(bool force = true)
        {
            if (!_wired)
            {
                TryWireElements();
            }

            if (!_wired)
            {
                return;
            }

            var stack = ResolveLayerStack();
            var visible = stack.VisibleCount;
            var total = stack.Count;
            if (!force
                && _refreshedOnce
                && visible == _lastLayerVisible
                && total == _lastLayerTotal
                && _bookmarkCount == _lastBookmarkCount)
            {
                ApplyPanelVisibility();
                return;
            }

            _items = C2MenuProjection.ProjectDefault(_bookmarkCount, stack);
            RebuildMenuList(_items);
            ApplySummary(stack);
            _lastLayerVisible = visible;
            _lastLayerTotal = total;
            _lastBookmarkCount = _bookmarkCount;
            _refreshedOnce = true;
            ApplyPanelVisibility();
        }

        private void ResolveMapHost()
        {
            if (mapHost != null)
            {
                return;
            }

            mapHost = FindFirstObjectByType<MapPlaceholderPanelHost>();
        }

        private MapLayerStackState ResolveLayerStack()
        {
            if (mapHost != null)
            {
                return mapHost.LayerStack ?? MapLayerStackState.WithDefaults();
            }

            return MapLayerStackState.WithDefaults();
        }

        private void TryWireElements()
        {
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            var panel = root.Q<VisualElement>(RootName) ?? root;
            _menuList = panel.Q<VisualElement>(ListName);
            _summaryLine = panel.Q<Label>(SummaryName);
            _statusLine = panel.Q<Label>(StatusName);
            _wired = _menuList != null;

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }
        }

        private void RebuildMenuList(IReadOnlyList<C2MenuItemEntry> items)
        {
            if (_menuList == null)
            {
                return;
            }

            _menuList.Clear();
            if (items == null || items.Count == 0)
            {
                SetStatus(null);
                return;
            }

            C2MenuCategory? lastCategory = null;
            string? firstStatusNote = null;

            foreach (var item in items)
            {
                if (item is null)
                {
                    continue;
                }

                if (lastCategory is null || lastCategory.Value != item.Category)
                {
                    lastCategory = item.Category;
                    var header = new Label(item.Category.ToString().ToUpperInvariant());
                    header.AddToClassList("c2-menu-category");
                    _menuList.Add(header);
                }

                var row = new VisualElement();
                row.AddToClassList("c2-menu-row");
                var isLayer = item.Id.StartsWith(LayerIdPrefix, StringComparison.Ordinal);
                if (isLayer)
                {
                    row.AddToClassList("c2-menu-row--layer");
                }

                if (!item.IsEnabled)
                {
                    row.AddToClassList("c2-menu-row--disabled");
                }

                var label = new Label(item.Label ?? string.Empty);
                label.AddToClassList("c2-menu-row-label");
                row.Add(label);

                // Inline shortcut discovery (CMD-28.1).
                var shortcut = new Label(item.ShortcutLabel ?? "none");
                shortcut.AddToClassList("c2-menu-row-shortcut");
                row.Add(shortcut);

                if (!string.IsNullOrWhiteSpace(item.StatusNote))
                {
                    firstStatusNote ??= item.StatusNote;
                    var note = new Label(item.StatusNote);
                    note.AddToClassList("c2-menu-row-note");
                    row.Add(note);
                }
                else if (!item.IsEnabled && !string.IsNullOrWhiteSpace(item.DisabledReason))
                {
                    var note = new Label(item.DisabledReason);
                    note.AddToClassList("c2-menu-row-note");
                    row.Add(note);
                }

                if (isLayer && item.IsEnabled)
                {
                    var capturedId = item.Id;
                    row.RegisterCallback<ClickEvent>(_ => OnLayerRowClicked(capturedId));
                }
                else if (item.IsEnabled && (item.Id == NextUnitId || item.Id == PrevUnitId))
                {
                    var forward = item.Id == NextUnitId;
                    row.RegisterCallback<ClickEvent>(_ => OnCycleUnitClicked(forward));
                }

                _menuList.Add(row);
            }

            SetStatus(firstStatusNote);
        }

        private void OnLayerRowClicked(string menuItemId)
        {
            if (!TryParseLayerId(menuItemId, out var layerId))
            {
                SetStatus($"UNKNOWN_LAYER:{menuItemId}");
                return;
            }

            ResolveMapHost();
            if (mapHost == null)
            {
                SetStatus("NO_MAP_HOST");
                return;
            }

            mapHost.ToggleLayer(layerId);
            // Rebuild so On/Off labels and summary refresh immediately.
            Refresh(force: true);
            SetStatus(null);
        }

        /// <summary>
        /// CMD-28.8/.10: cycle the C2 unit selection forward/backward through alive OOB units.
        /// UI-local navigation — reuses <see cref="DelegationBridgeHost.SelectUnit"/>, the same
        /// friendly-unit selection path map symbol clicks and OOB tree rows already use.
        /// </summary>
        private void OnCycleUnitClicked(bool forward)
        {
            if (bridgeHost == null)
            {
                SetStatus("NO_BRIDGE");
                return;
            }

            var aliveUnitIds = new List<string>();
            foreach (var entry in bridgeHost.LastOobTree)
            {
                if (entry != null && entry.IsAlive && !string.IsNullOrWhiteSpace(entry.UnitId))
                {
                    aliveUnitIds.Add(entry.UnitId);
                }
            }

            if (aliveUnitIds.Count == 0)
            {
                SetStatus("NO_UNITS");
                return;
            }

            var currentIndex = bridgeHost.SelectedUnitId != null
                ? aliveUnitIds.IndexOf(bridgeHost.SelectedUnitId)
                : -1;
            var step = forward ? 1 : -1;
            var nextIndex = currentIndex < 0
                ? 0
                : (currentIndex + step + aliveUnitIds.Count) % aliveUnitIds.Count;

            bridgeHost.SelectUnit(aliveUnitIds[nextIndex]);
            SetStatus(null);
        }

        private static bool TryParseLayerId(string menuItemId, out MapLayerId layerId)
        {
            layerId = default;
            if (string.IsNullOrWhiteSpace(menuItemId)
                || !menuItemId.StartsWith(LayerIdPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            var name = menuItemId.Substring(LayerIdPrefix.Length);
            return Enum.TryParse(name, ignoreCase: false, out layerId);
        }

        private void ApplySummary(MapLayerStackState stack)
        {
            var presentation = MapLayerStackApplyState.Apply(stack);
            LastSummaryLabel = presentation.SummaryLabel;
            if (_summaryLine != null)
            {
                _summaryLine.text = presentation.SummaryLabel;
            }
        }

        private void SetStatus(string? status)
        {
            LastStatus = status;
            if (_statusLine != null)
            {
                _statusLine.text = status ?? string.Empty;
            }
        }

        private void ApplyPanelVisibility()
        {
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
#endif
