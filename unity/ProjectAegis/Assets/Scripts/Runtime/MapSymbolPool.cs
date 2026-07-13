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
    /// for every symbol, every frame). <see cref="Sync"/> itself is only invoked by the host on
    /// dirty ticks (see <c>MapPlaceholderPanelHost.IsDirty</c>), so every call re-applies content —
    /// this is still far cheaper than the pre-pooling full rebuild because no
    /// <see cref="VisualElement"/> is destroyed/recreated and no click callback is re-registered for
    /// existing ids; only the (cheap) style/content diff of <see cref="Apply"/> runs.
    /// </summary>
    /// <remarks>
    /// req 20 rev 2 Track T4 (symbology): <see cref="Sync"/> additionally takes the current icon size
    /// (px, from <c>MapIconSizeLadder.ResolveIconSizePx</c>), label visibility (zoom-band gated), and
    /// the per-symbol label-declutter outcome map (<c>MapLabelDeclutterProjection</c>) so the pooled
    /// content mirrors what the pre-pooling <c>RebuildSymbols</c>/<c>BuildLabel</c> implementation
    /// produced — icon sizing, the domain-modifier badge, and label declutter are cross-row
    /// computations the host resolves once per dirty tick and hands down here.
    /// </remarks>
    public sealed class MapSymbolPool
    {
        private const string SymbolClass = "map-symbol";
        private const string DomainModifierName = "domain-modifier";
        private const string LabelHiddenClass = "map-symbol__label--hidden";
        private const string LabelLeaderLineClass = "map-symbol__label--leader-line";

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
        /// detach for removed ids. <paramref name="iconSizePx"/> / <paramref name="labelsVisible"/> /
        /// <paramref name="declutter"/> are T4 symbology inputs applied to every row on every call
        /// (Sync is already dirty-gated by the host, so recomputation here is cheap and correct even
        /// when declutter/selection changes affect rows whose own data is otherwise unchanged).
        /// </summary>
        public void Sync(
            IReadOnlyList<MapSymbolDisplayRow> rows,
            Action<string> onSymbolClicked,
            float iconSizePx,
            bool labelsVisible,
            IReadOnlyDictionary<string, MapLabelDeclutterOutcome> declutter)
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

                declutter.TryGetValue(row.SymbolId, out var outcome);
                Apply(entry.Container, entry.Applied, row, iconSizePx, labelsVisible, row.IsGhost ? null : outcome);
                entry.Applied = row;
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

        private static void Apply(
            VisualElement container,
            MapSymbolDisplayRow? previous,
            MapSymbolDisplayRow row,
            float iconSizePx,
            bool labelsVisible,
            MapLabelDeclutterOutcome? labelOutcome)
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

            // Rebuild the small content (atlas frame or glyph, domain modifier, then label). Bounded
            // by actual change since Sync only calls Apply on dirty ticks.
            container.Clear();

            // Icon/frame shape is always shown regardless of declutter — only the text label is
            // decluttered (a11y §5: affiliation must remain shape-readable at every zoom band).
            if (row.UsesAtlasFrame && !string.IsNullOrEmpty(row.AtlasFrameClass))
            {
                var frame = new VisualElement
                {
                    style = { width = iconSizePx, height = iconSizePx },
                };
                frame.AddToClassList(row.AtlasFrameClass);
                container.Add(frame);
            }
            else if (!string.IsNullOrEmpty(row.Glyph))
            {
                container.Add(new Label(row.Glyph) { style = { fontSize = iconSizePx } });
            }

            // Domain modifier (Air/Surface/Subsurface/Land/Mine/Facility) — secondary cue layered
            // on the frame, never a substitute for the affiliation shape above.
            if (!string.IsNullOrEmpty(row.DomainModifierClass))
            {
                var domainBadge = new VisualElement { name = DomainModifierName };
                domainBadge.AddToClassList(row.DomainModifierClass);
                container.Add(domainBadge);
            }

            container.Add(BuildLabel(row, labelsVisible, labelOutcome));
            container.pickingMode = row.IsGhost ? PickingMode.Ignore : PickingMode.Position;
        }

        /// <summary>
        /// Build the text label element for a symbol row, applying the zoom-band visibility rule
        /// ("labels appear from regional zoom") and the declutter outcome (shown / leader-line /
        /// hidden) for the current frame. Ghost rows (comms-degraded duplicates) are exempt from
        /// declutter — <paramref name="labelOutcome"/> is <c>null</c> for them (see <see cref="Sync"/>)
        /// and they already carry their own lag-offset visual language.
        /// </summary>
        private static Label BuildLabel(MapSymbolDisplayRow row, bool labelsVisible, MapLabelDeclutterOutcome? labelOutcome)
        {
            var label = new Label(row.Label);
            if (!labelsVisible)
            {
                label.AddToClassList(LabelHiddenClass);
                return label;
            }

            if (labelOutcome == null)
            {
                return label;
            }

            switch (labelOutcome.Value)
            {
                case MapLabelDeclutterOutcome.Hidden:
                    label.AddToClassList(LabelHiddenClass);
                    break;
                case MapLabelDeclutterOutcome.LeaderLine:
                    label.AddToClassList(LabelLeaderLineClass);
                    break;
                case MapLabelDeclutterOutcome.Shown:
                default:
                    break;
            }

            return label;
        }

        private static IEnumerable<string> Tokens(string styleClass) =>
            string.IsNullOrEmpty(styleClass)
                ? Array.Empty<string>()
                : styleClass.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }
}
#endif
