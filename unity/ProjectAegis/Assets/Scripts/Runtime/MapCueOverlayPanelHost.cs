// DRG-712 ([C2-17 UI]): companion non-blocking overlay for stale/datalink contact icons & queued-order badges.
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MapCueOverlayPanelHost : MonoBehaviour
    {
        private const int DefaultOverlaySortingOrder = 6;
        private const string SymbolContainerClass = "map-cue-symbol";
        private const string ChipClass = "map-cue-chip";

        private static readonly string[] AllCueClasses =
        {
            MapCueOverlayProjection.StaleCssClass,
            MapCueOverlayProjection.DatalinkCssClass,
            MapCueOverlayProjection.QueuedCssClass,
            MapCueOverlayProjection.FixtureTagCssClass,
        };

        private sealed class PooledCueEntry
        {
            public VisualElement Container = null!;
            public Label ContactChip = null!;
            public Label UnitChip = null!;
            public MapSymbolCueRow? Applied;
        }

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private VisualElement? _root;
        private VisualElement? _selectedStrip;
        private Label? _contactBadge;
        private Label? _contactFixture;
        private Label? _unitOrderBadge;
        private VisualElement? _canvas;
        private bool _wired;

        private readonly Dictionary<string, PooledCueEntry> _pool = new(StringComparer.Ordinal);
        private readonly List<string> _stalePoolIds = new();
        private MapCueOverlayState _lastState = MapCueOverlayState.Empty;

        /// <summary>Last projected cue overlay state (headless-readable after Refresh).</summary>
        public MapCueOverlayState LastState => _lastState;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            UiDocumentPanelSettingsBootstrap.EnsureDocument(_document);
            if (_document.sortingOrder <= 0 || _document.sortingOrder >= 10)
            {
                _document.sortingOrder = DefaultOverlaySortingOrder;
            }

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
                if (_root != null && !showPanel)
                {
                    _root.style.display = DisplayStyle.None;
                }

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
            var docRoot = _document.rootVisualElement;
            if (docRoot == null)
            {
                return;
            }

            docRoot.pickingMode = PickingMode.Ignore;
            _root = docRoot.Q<VisualElement>(MapCueOverlayProjection.RootElementName) ?? docRoot;
            _selectedStrip = _root.Q<VisualElement>(MapCueOverlayProjection.SelectedStripElementName);
            _contactBadge = _root.Q<Label>(MapCueOverlayProjection.SelectedContactBadgeElementName);
            _contactFixture = _root.Q<Label>(MapCueOverlayProjection.SelectedContactFixtureElementName);
            _unitOrderBadge = _root.Q<Label>(MapCueOverlayProjection.SelectedUnitOrderBadgeElementName);
            _canvas = _root.Q<VisualElement>(MapCueOverlayProjection.CanvasElementName);

            ApplyIgnorePicking(_root);

            if (panelStyles != null && !_root.styleSheets.Contains(panelStyles))
            {
                _root.styleSheets.Add(panelStyles);
            }

            _wired = _canvas != null;
        }

        public void Refresh()
        {
            if (!_wired)
            {
                return;
            }

            if (!showPanel || bridgeHost == null || bridgeHost.Bridge == null)
            {
                _lastState = MapCueOverlayState.Empty;
                ClearAllVisuals();
                return;
            }

            var isInspecting = bridgeHost.CommandTimeline.IsInspecting;
            var symbols = isInspecting ? bridgeHost.DisplayCombatSymbols : bridgeHost.LastMapSymbols;
            var state = MapCueOverlayProjection.Project(
                bridgeHost.Bridge,
                symbols,
                bridgeHost.CurrentSimTick,
                bridgeHost.SelectedContactId,
                bridgeHost.SelectedUnitId,
                isInspecting);

            _lastState = state;

            if (_root != null)
            {
                _root.style.display = state.IsInspectingHistory ? DisplayStyle.None : DisplayStyle.Flex;
            }

            ApplySelectedContactCue(state.SelectedContactCue);
            ApplySelectedUnitOrderCue(state.SelectedUnitOrderCue);
            SyncSymbolPool(state.Symbols);
        }

        private void ClearAllVisuals()
        {
            ApplySelectedContactCue(SelectedContactCueState.Empty);
            ApplySelectedUnitOrderCue(SelectedUnitOrderCueState.Empty);
            SyncSymbolPool(Array.Empty<MapSymbolCueRow>());
            if (_root != null)
            {
                _root.style.display = DisplayStyle.None;
            }
        }

        private void ApplySelectedContactCue(SelectedContactCueState cue)
        {
            if (_contactBadge != null)
            {
                _contactBadge.pickingMode = PickingMode.Ignore;
                ClearCueClasses(_contactBadge);
                if (!cue.Visible || string.IsNullOrEmpty(cue.Text))
                {
                    _contactBadge.text = string.Empty;
                    _contactBadge.style.display = DisplayStyle.None;
                }
                else
                {
                    _contactBadge.text = cue.Text;
                    _contactBadge.style.display = DisplayStyle.Flex;
                    for (var i = 0; i < cue.CssClasses.Count; i++)
                    {
                        _contactBadge.AddToClassList(cue.CssClasses[i]);
                    }
                }
            }

            if (_contactFixture != null)
            {
                _contactFixture.pickingMode = PickingMode.Ignore;
                ClearCueClasses(_contactFixture);
                if (!cue.Visible || string.IsNullOrEmpty(cue.FixtureDisclosureText))
                {
                    _contactFixture.text = string.Empty;
                    _contactFixture.style.display = DisplayStyle.None;
                }
                else
                {
                    _contactFixture.text = cue.FixtureDisclosureText;
                    _contactFixture.style.display = DisplayStyle.Flex;
                    _contactFixture.AddToClassList(MapCueOverlayProjection.FixtureTagCssClass);
                }
            }
        }

        private void ApplySelectedUnitOrderCue(SelectedUnitOrderCueState cue)
        {
            if (_unitOrderBadge == null)
            {
                return;
            }

            _unitOrderBadge.pickingMode = PickingMode.Ignore;
            ClearCueClasses(_unitOrderBadge);
            if (!cue.Visible || string.IsNullOrEmpty(cue.Text))
            {
                _unitOrderBadge.text = string.Empty;
                _unitOrderBadge.style.display = DisplayStyle.None;
                return;
            }

            _unitOrderBadge.text = cue.Text;
            _unitOrderBadge.style.display = DisplayStyle.Flex;
            for (var i = 0; i < cue.CssClasses.Count; i++)
            {
                _unitOrderBadge.AddToClassList(cue.CssClasses[i]);
            }
        }

        private void SyncSymbolPool(IReadOnlyList<MapSymbolCueRow> rows)
        {
            if (_canvas == null)
            {
                return;
            }

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (!_pool.TryGetValue(row.SymbolId, out var entry))
                {
                    entry = CreatePooledEntry(row.SymbolId);
                    _pool[row.SymbolId] = entry;
                    _canvas.Add(entry.Container);
                }

                if (!RowsEqual(entry.Applied, row))
                {
                    ApplyPooledEntry(entry, row);
                    entry.Applied = row;
                }
            }

            _stalePoolIds.Clear();
            foreach (var kvp in _pool)
            {
                if (!ContainsSymbolId(rows, kvp.Key))
                {
                    _stalePoolIds.Add(kvp.Key);
                }
            }

            for (var i = 0; i < _stalePoolIds.Count; i++)
            {
                var id = _stalePoolIds[i];
                _pool[id].Container.RemoveFromHierarchy();
                _pool.Remove(id);
            }
        }

        private static PooledCueEntry CreatePooledEntry(string symbolId)
        {
            var container = new VisualElement
            {
                name = MapCueOverlayProjection.SymbolElementName(symbolId),
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                },
            };
            container.AddToClassList(SymbolContainerClass);

            var contactChip = new Label
            {
                pickingMode = PickingMode.Ignore,
            };
            contactChip.AddToClassList(ChipClass);

            var unitChip = new Label
            {
                pickingMode = PickingMode.Ignore,
            };
            unitChip.AddToClassList(ChipClass);

            container.Add(contactChip);
            container.Add(unitChip);

            return new PooledCueEntry
            {
                Container = container,
                ContactChip = contactChip,
                UnitChip = unitChip,
            };
        }

        private static void ApplyPooledEntry(PooledCueEntry entry, MapSymbolCueRow row)
        {
            entry.Container.style.left = Length.Percent(row.NormalizedX * 100f);
            entry.Container.style.top = Length.Percent(row.NormalizedY * 100f);
            entry.Container.pickingMode = PickingMode.Ignore;
            entry.Container.tooltip = row.TooltipText;
            ClearCueClasses(entry.Container);
            for (var i = 0; i < row.CssClasses.Count; i++)
            {
                entry.Container.AddToClassList(row.CssClasses[i]);
            }

            ClearCueClasses(entry.ContactChip);
            if (!string.IsNullOrEmpty(row.ContactTokensText))
            {
                entry.ContactChip.text = row.ContactTokensText;
                entry.ContactChip.style.display = DisplayStyle.Flex;
                if (row.IsStale)
                {
                    entry.ContactChip.AddToClassList(MapCueOverlayProjection.StaleCssClass);
                }

                if (row.IsDatalinkSourced)
                {
                    entry.ContactChip.AddToClassList(MapCueOverlayProjection.DatalinkCssClass);
                }
            }
            else
            {
                entry.ContactChip.text = string.Empty;
                entry.ContactChip.style.display = DisplayStyle.None;
            }

            ClearCueClasses(entry.UnitChip);
            if (!string.IsNullOrEmpty(row.UnitBadgeText))
            {
                entry.UnitChip.text = row.UnitBadgeText;
                entry.UnitChip.style.display = DisplayStyle.Flex;
                entry.UnitChip.AddToClassList(MapCueOverlayProjection.QueuedCssClass);
            }
            else
            {
                entry.UnitChip.text = string.Empty;
                entry.UnitChip.style.display = DisplayStyle.None;
            }

            var hasAnyCue = !string.IsNullOrEmpty(row.ContactTokensText) || !string.IsNullOrEmpty(row.UnitBadgeText);
            entry.Container.style.display = hasAnyCue ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static bool ContainsSymbolId(IReadOnlyList<MapSymbolCueRow> rows, string id)
        {
            for (var i = 0; i < rows.Count; i++)
            {
                if (string.Equals(rows[i].SymbolId, id, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool RowsEqual(MapSymbolCueRow? a, MapSymbolCueRow b)
        {
            if (a is null)
            {
                return false;
            }

            return string.Equals(a.SymbolId, b.SymbolId, StringComparison.Ordinal)
                && a.NormalizedX.Equals(b.NormalizedX)
                && a.NormalizedY.Equals(b.NormalizedY)
                && a.IsStale == b.IsStale
                && a.IsDatalinkSourced == b.IsDatalinkSourced
                && a.IsQueuedOrder == b.IsQueuedOrder
                && string.Equals(a.ContactTokensText, b.ContactTokensText, StringComparison.Ordinal)
                && string.Equals(a.UnitBadgeText, b.UnitBadgeText, StringComparison.Ordinal)
                && string.Equals(a.FixtureLabel, b.FixtureLabel, StringComparison.Ordinal)
                && string.Equals(a.TooltipText, b.TooltipText, StringComparison.Ordinal);
        }

        private static void ClearCueClasses(VisualElement element)
        {
            for (var i = 0; i < AllCueClasses.Length; i++)
            {
                element.RemoveFromClassList(AllCueClasses[i]);
            }
        }

        private static void ApplyIgnorePicking(VisualElement root)
        {
            root.pickingMode = PickingMode.Ignore;
            foreach (var child in root.Children())
            {
                ApplyIgnorePicking(child);
            }
        }
    }
}
#endif
