// Pooled tactical-map symbol renderer (fix #3, unity-integration-review).
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    /// <summary>
    /// Pools tactical-map symbol <see cref="VisualElement"/>s keyed by symbol id so a per-tick
    /// refresh reuses elements instead of clearing and recreating the whole canvas every frame
    /// (the previous <c>RebuildSymbols</c> allocated a container + labels + a fresh click callback
    /// for every symbol, every frame). Only rows whose <see cref="MapSymbolDisplayRow"/> value
    /// actually changed are re-applied; ids that disappear are detached; new ids are created once.
    /// </summary>
    public sealed class MapSymbolPool
    {
        private const string SymbolClass = "map-symbol";

        private sealed class Entry
        {
            public VisualElement Container = null!;
            public MapSymbolDisplayRow? Applied;
        }

        private readonly VisualElement _canvas;
        private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
        private readonly List<string> _stale = new();

        public MapSymbolPool(VisualElement canvas)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        }

        /// <summary>Live pooled symbol count (exposed for tests / diagnostics).</summary>
        public int Count => _entries.Count;

        /// <summary>
        /// Reconciles the canvas to <paramref name="rows"/>: reuse-in-place for unchanged/moved
        /// symbols, create for new ids (registering <paramref name="onSymbolClicked"/> once), and
        /// detach for removed ids.
        /// </summary>
        public void Sync(IReadOnlyList<MapSymbolDisplayRow> rows, Action<string> onSymbolClicked)
        {
            if (rows == null)
            {
                return;
            }

            var structuralChange = false;

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (!_entries.TryGetValue(row.SymbolId, out var entry))
                {
                    entry = new Entry { Container = CreateContainer(row.SymbolId, onSymbolClicked) };
                    _entries[row.SymbolId] = entry;
                    _canvas.Add(entry.Container);
                    structuralChange = true;
                }

                if (entry.Applied != row)
                {
                    Apply(entry.Container, entry.Applied, row);
                    entry.Applied = row;
                }
            }

            _stale.Clear();
            foreach (var kvp in _entries)
            {
                if (!ContainsId(rows, kvp.Key))
                {
                    _stale.Add(kvp.Key);
                }
            }

            foreach (var id in _stale)
            {
                _entries[id].Container.RemoveFromHierarchy();
                _entries.Remove(id);
                structuralChange = true;
            }

            // Keep sibling (draw) order aligned to row order only when the set actually changed —
            // steady-state re-syncs touch no hierarchy order.
            if (structuralChange)
            {
                for (var i = 0; i < rows.Count; i++)
                {
                    if (_entries.TryGetValue(rows[i].SymbolId, out var entry))
                    {
                        entry.Container.BringToFront();
                    }
                }
            }
        }

        /// <summary>Detach all pooled elements (e.g. when the canvas is rewired).</summary>
        public void Clear()
        {
            _canvas.Clear();
            _entries.Clear();
        }

        private static bool ContainsId(IReadOnlyList<MapSymbolDisplayRow> rows, string id)
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

        private static VisualElement CreateContainer(string symbolId, Action<string> onSymbolClicked)
        {
            var container = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                },
                userData = symbolId,
            };
            container.AddToClassList(SymbolClass);
            // Registered once for the element's lifetime; ghost rows are gated via pickingMode in Apply.
            container.RegisterCallback<ClickEvent>(_ => onSymbolClicked?.Invoke(symbolId));
            return container;
        }

        private static void Apply(VisualElement container, MapSymbolDisplayRow? previous, MapSymbolDisplayRow row)
        {
            container.style.left = Length.Percent(row.NormalizedX * 100f);
            container.style.top = Length.Percent(row.NormalizedY * 100f);

            // Swap the dynamic style-class tokens, preserving the base SymbolClass.
            if (previous != null)
            {
                foreach (var token in Tokens(previous.StyleClass))
                {
                    container.RemoveFromClassList(token);
                }
            }

            foreach (var token in Tokens(row.StyleClass))
            {
                container.AddToClassList(token);
            }

            // Rebuild the small content (atlas frame or glyph, then label). Bounded by actual change.
            container.Clear();
            if (row.UsesAtlasFrame && !string.IsNullOrEmpty(row.AtlasFrameClass))
            {
                var frame = new VisualElement();
                frame.AddToClassList(row.AtlasFrameClass);
                container.Add(frame);
            }
            else if (!string.IsNullOrEmpty(row.Glyph))
            {
                container.Add(new Label(row.Glyph));
            }

            container.Add(new Label(row.Label));
            container.pickingMode = row.IsGhost ? PickingMode.Ignore : PickingMode.Position;
        }

        private static IEnumerable<string> Tokens(string styleClass) =>
            string.IsNullOrEmpty(styleClass)
                ? Array.Empty<string>()
                : styleClass.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }
}
#endif
