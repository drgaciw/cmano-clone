// DRG-408: companion non-blocking overlay for map notification balloons.
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
    public sealed class MapBalloonOverlayPanelHost : MonoBehaviour
    {
        private const int DefaultOverlaySortingOrder = 7;
        private const string CardClass = "map-balloon-card";
        private const string HeaderClass = "map-balloon-card-header";
        private const string BodyClass = "map-balloon-card-body";

        private sealed class PooledBalloonEntry
        {
            public VisualElement Container = null!;
            public Label Header = null!;
            public Label Body = null!;
            public MapBalloonOverlayRow? Applied;
        }

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private VisualElement? _root;
        private VisualElement? _unanchoredStrip;
        private VisualElement? _canvas;
        private bool _wired;

        private readonly Dictionary<string, PooledBalloonEntry> _anchoredPool = new(StringComparer.Ordinal);
        private readonly Dictionary<string, PooledBalloonEntry> _unanchoredPool = new(StringComparer.Ordinal);
        private readonly List<string> _staleIds = new();
        private MapBalloonOverlayState _lastState = MapBalloonOverlayState.Empty;

        public MapBalloonOverlayState LastState => _lastState;

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
            _root = docRoot.Q<VisualElement>(MapBalloonOverlayProjection.RootElementName) ?? docRoot;
            _unanchoredStrip = _root.Q<VisualElement>(MapBalloonOverlayProjection.UnanchoredStripElementName);
            _canvas = _root.Q<VisualElement>(MapBalloonOverlayProjection.CanvasElementName);

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
                _lastState = MapBalloonOverlayState.Empty;
                ClearAllVisuals();
                return;
            }

            var isInspecting = bridgeHost.CommandTimeline.IsInspecting;
            var symbols = isInspecting ? bridgeHost.DisplayCombatSymbols : bridgeHost.LastMapSymbols;
            var state = MapBalloonOverlayProjection.Project(
                bridgeHost.ActiveMapBalloons,
                symbols,
                isInspecting);
            _lastState = state;

            if (_root != null)
            {
                _root.style.display = state.IsInspectingHistory ? DisplayStyle.None : DisplayStyle.Flex;
            }

            SyncPool(_canvas, _anchoredPool, state.AnchoredBalloons, anchored: true);
            SyncPool(_unanchoredStrip, _unanchoredPool, state.UnanchoredBalloons, anchored: false);
        }

        private void ClearAllVisuals()
        {
            SyncPool(_canvas, _anchoredPool, Array.Empty<MapBalloonOverlayRow>(), anchored: true);
            SyncPool(_unanchoredStrip, _unanchoredPool, Array.Empty<MapBalloonOverlayRow>(), anchored: false);
            if (_root != null)
            {
                _root.style.display = DisplayStyle.None;
            }
        }

        private void SyncPool(
            VisualElement? parent,
            Dictionary<string, PooledBalloonEntry> pool,
            IReadOnlyList<MapBalloonOverlayRow> rows,
            bool anchored)
        {
            if (parent == null)
            {
                return;
            }

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (!pool.TryGetValue(row.BalloonId, out var entry))
                {
                    entry = CreatePooledEntry(row.BalloonId, anchored);
                    pool[row.BalloonId] = entry;
                    parent.Add(entry.Container);
                }

                if (!RowsEqual(entry.Applied, row))
                {
                    ApplyPooledEntry(entry, row, anchored);
                    entry.Applied = row;
                }
            }

            _staleIds.Clear();
            foreach (var kvp in pool)
            {
                if (!ContainsBalloonId(rows, kvp.Key))
                {
                    _staleIds.Add(kvp.Key);
                }
            }

            for (var i = 0; i < _staleIds.Count; i++)
            {
                var id = _staleIds[i];
                pool[id].Container.RemoveFromHierarchy();
                pool.Remove(id);
            }
        }

        private static PooledBalloonEntry CreatePooledEntry(string balloonId, bool anchored)
        {
            var container = new VisualElement
            {
                name = MapBalloonOverlayProjection.BalloonElementName(balloonId),
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = anchored ? Position.Absolute : Position.Relative,
                    flexDirection = FlexDirection.Column,
                },
            };
            container.AddToClassList(CardClass);

            var header = new Label { pickingMode = PickingMode.Ignore };
            header.AddToClassList(HeaderClass);

            var body = new Label { pickingMode = PickingMode.Ignore };
            body.AddToClassList(BodyClass);

            container.Add(header);
            container.Add(body);
            return new PooledBalloonEntry
            {
                Container = container,
                Header = header,
                Body = body,
            };
        }

        private static void ApplyPooledEntry(PooledBalloonEntry entry, MapBalloonOverlayRow row, bool anchored)
        {
            entry.Container.pickingMode = PickingMode.Ignore;
            entry.Container.RemoveFromClassList(MapBalloonOverlayProjection.PauseBalloonCssClass);
            entry.Container.RemoveFromClassList(MapBalloonOverlayProjection.AlertBalloonCssClass);
            entry.Container.AddToClassList(row.CssClass);

            if (anchored)
            {
                entry.Container.style.left = Length.Percent(row.NormalizedX * 100f);
                entry.Container.style.top = Length.Percent(row.NormalizedY * 100f);
            }

            var prefix = row.IsPauseTriggered ? "[PAUSE]" : "[ALERT]";
            entry.Header.text = string.IsNullOrEmpty(row.SubjectId)
                ? $"{prefix} {row.Category}"
                : $"{prefix} {row.Category} · {row.SubjectId}";
            entry.Body.text = row.Text;
        }

        private static bool ContainsBalloonId(IReadOnlyList<MapBalloonOverlayRow> rows, string id)
        {
            for (var i = 0; i < rows.Count; i++)
            {
                if (string.Equals(rows[i].BalloonId, id, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool RowsEqual(MapBalloonOverlayRow? a, MapBalloonOverlayRow b)
        {
            if (a is null)
            {
                return false;
            }

            return string.Equals(a.BalloonId, b.BalloonId, StringComparison.Ordinal)
                && a.NormalizedX.Equals(b.NormalizedX)
                && a.NormalizedY.Equals(b.NormalizedY)
                && a.HasMapAnchor == b.HasMapAnchor
                && a.IsPauseTriggered == b.IsPauseTriggered
                && string.Equals(a.Category, b.Category, StringComparison.Ordinal)
                && string.Equals(a.SubjectId, b.SubjectId, StringComparison.Ordinal)
                && string.Equals(a.Text, b.Text, StringComparison.Ordinal);
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
