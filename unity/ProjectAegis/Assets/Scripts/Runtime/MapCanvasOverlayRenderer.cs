// Pooled map-canvas overlay renderer for envelope rings + datalink edges (DRG-160).
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    /// <summary>
    /// Draws projected <see cref="MapCanvasRingShape"/> / <see cref="MapCanvasEdgeShape"/> lists
    /// onto the map canvas <see cref="VisualElement"/>. Rings render behind edges; both render
    /// behind unit symbols (host inserts this layer first).
    /// </summary>
    public sealed class MapCanvasOverlayRenderer
    {
        private const string RingLayerName = "map-overlay-ring-layer";
        private const string EdgeLayerName = "map-overlay-edge-layer";
        private const string RingBaseClass = "map-overlay-ring";
        private const string EdgeBaseClass = "map-overlay-edge";

        private sealed class RingEntry
        {
            public VisualElement Element = null!;
            public MapCanvasRingShape? Applied;
        }

        private sealed class EdgeEntry
        {
            public VisualElement Element = null!;
            public MapCanvasEdgeShape? Applied;
        }

        private readonly VisualElement _ringLayer;
        private readonly VisualElement _edgeLayer;
        private readonly Dictionary<string, RingEntry> _rings = new(StringComparer.Ordinal);
        private readonly Dictionary<string, EdgeEntry> _edges = new(StringComparer.Ordinal);
        private readonly List<string> _stale = new();

        public MapCanvasOverlayRenderer(VisualElement canvas)
        {
            if (canvas == null)
            {
                throw new ArgumentNullException(nameof(canvas));
            }

            _ringLayer = new VisualElement { name = RingLayerName, pickingMode = PickingMode.Ignore };
            _ringLayer.AddToClassList("map-overlay-layer");
            _edgeLayer = new VisualElement { name = EdgeLayerName, pickingMode = PickingMode.Ignore };
            _edgeLayer.AddToClassList("map-overlay-layer");
            canvas.Insert(0, _ringLayer);
            canvas.Insert(1, _edgeLayer);
        }

        /// <summary>Live pooled ring count (diagnostics / tests).</summary>
        public int RingCount => _rings.Count;

        /// <summary>Live pooled edge count (diagnostics / tests).</summary>
        public int EdgeCount => _edges.Count;

        /// <summary>
        /// Reconciles ring/edge layers to the projected shape lists. Reuses elements in-place
        /// when shape values are unchanged.
        /// </summary>
        public void Sync(
            IReadOnlyList<MapCanvasRingShape> rings,
            IReadOnlyList<MapCanvasEdgeShape> edges)
        {
            SyncRings(rings ?? Array.Empty<MapCanvasRingShape>());
            SyncEdges(edges ?? Array.Empty<MapCanvasEdgeShape>());
        }

        /// <summary>Detach all overlay elements.</summary>
        public void Clear()
        {
            _ringLayer.Clear();
            _edgeLayer.Clear();
            _rings.Clear();
            _edges.Clear();
        }

        private void SyncRings(IReadOnlyList<MapCanvasRingShape> rings)
        {
            for (var i = 0; i < rings.Count; i++)
            {
                var shape = rings[i];
                if (!_rings.TryGetValue(shape.Key, out var entry))
                {
                    entry = new RingEntry { Element = CreateRingElement(shape.Key) };
                    _rings[shape.Key] = entry;
                    _ringLayer.Add(entry.Element);
                }

                if (entry.Applied != shape)
                {
                    ApplyRing(entry.Element, entry.Applied, shape);
                    entry.Applied = shape;
                }
            }

            PruneRingStale(rings);
        }

        private void SyncEdges(IReadOnlyList<MapCanvasEdgeShape> edges)
        {
            for (var i = 0; i < edges.Count; i++)
            {
                var shape = edges[i];
                if (!_edges.TryGetValue(shape.Key, out var entry))
                {
                    entry = new EdgeEntry { Element = CreateEdgeElement(shape.Key) };
                    _edges[shape.Key] = entry;
                    _edgeLayer.Add(entry.Element);
                }

                if (entry.Applied != shape)
                {
                    ApplyEdge(entry.Element, entry.Applied, shape);
                    entry.Applied = shape;
                }
            }

            PruneEdgeStale(edges);
        }

        private void PruneRingStale(IReadOnlyList<MapCanvasRingShape> live)
        {
            _stale.Clear();
            foreach (var kvp in _rings)
            {
                if (!ContainsRingKey(live, kvp.Key))
                {
                    _stale.Add(kvp.Key);
                }
            }

            foreach (var key in _stale)
            {
                _rings[key].Element.RemoveFromHierarchy();
                _rings.Remove(key);
            }
        }

        private void PruneEdgeStale(IReadOnlyList<MapCanvasEdgeShape> live)
        {
            _stale.Clear();
            foreach (var kvp in _edges)
            {
                if (!ContainsEdgeKey(live, kvp.Key))
                {
                    _stale.Add(kvp.Key);
                }
            }

            foreach (var key in _stale)
            {
                _edges[key].Element.RemoveFromHierarchy();
                _edges.Remove(key);
            }
        }

        private static bool ContainsRingKey(IReadOnlyList<MapCanvasRingShape> shapes, string key)
        {
            for (var i = 0; i < shapes.Count; i++)
            {
                if (string.Equals(shapes[i].Key, key, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsEdgeKey(IReadOnlyList<MapCanvasEdgeShape> shapes, string key)
        {
            for (var i = 0; i < shapes.Count; i++)
            {
                if (string.Equals(shapes[i].Key, key, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static VisualElement CreateRingElement(string key)
        {
            var element = new VisualElement
            {
                userData = key,
                pickingMode = PickingMode.Ignore,
            };
            element.AddToClassList(RingBaseClass);
            return element;
        }

        private static VisualElement CreateEdgeElement(string key)
        {
            var element = new VisualElement
            {
                userData = key,
                pickingMode = PickingMode.Ignore,
            };
            element.AddToClassList(EdgeBaseClass);
            return element;
        }

        private static void ApplyRing(VisualElement element, MapCanvasRingShape? previous, MapCanvasRingShape shape)
        {
            var diameterPct = shape.RadiusNormalized * 200f;
            element.style.position = Position.Absolute;
            element.style.width = Length.Percent(diameterPct);
            element.style.height = Length.Percent(diameterPct);
            element.style.left = Length.Percent((shape.CenterX - shape.RadiusNormalized) * 100f);
            element.style.top = Length.Percent((shape.CenterY - shape.RadiusNormalized) * 100f);
            element.style.borderTopLeftRadius = Length.Percent(50f);
            element.style.borderTopRightRadius = Length.Percent(50f);
            element.style.borderBottomLeftRadius = Length.Percent(50f);
            element.style.borderBottomRightRadius = Length.Percent(50f);

            if (previous != null)
            {
                element.RemoveFromClassList(previous.StyleClass);
            }

            element.AddToClassList(shape.StyleClass);
        }

        private static void ApplyEdge(VisualElement element, MapCanvasEdgeShape? previous, MapCanvasEdgeShape shape)
        {
            var dx = shape.ToX - shape.FromX;
            var dy = shape.ToY - shape.FromY;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length <= 1e-6f)
            {
                element.style.display = DisplayStyle.None;
                return;
            }

            element.style.display = DisplayStyle.Flex;
            element.style.position = Position.Absolute;
            element.style.left = Length.Percent(shape.FromX * 100f);
            element.style.top = Length.Percent(shape.FromY * 100f);
            element.style.width = Length.Percent(length * 100f);
            element.style.height = 2;
            element.style.transformOrigin = new TransformOrigin(Length.Percent(0f), Length.Percent(50f), 0);
            var angleDeg = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            element.style.rotate = new Rotate(angleDeg);

            if (previous != null)
            {
                element.RemoveFromClassList(previous.StyleClass);
            }

            element.AddToClassList(shape.StyleClass);
        }
    }
}
#endif
