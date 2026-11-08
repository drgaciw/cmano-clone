#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    /// <summary>Read-only pooled map overlay for explicitly authored normalized coverage polygons.</summary>
    public sealed class CoverageMapView : IDisposable
    {
        public const int MaxPolygons = 100;
        public const int MaxSegments = 2048;

        private readonly VisualElement _canvas;
        private readonly VisualElement _layer;
        private readonly Label _status;
        private readonly List<VisualElement> _segments = new();
        private readonly List<Label> _labels = new();
        private CoordinationSnapshot? _snapshot;
        private bool _visible;

        /// <summary>Creates a coverage layer attached to the supplied map canvas.</summary>
        public CoverageMapView(VisualElement canvas)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            _layer = new VisualElement { name = "coverage-map-layer", pickingMode = PickingMode.Ignore };
            _layer.style.position = Position.Absolute;
            _layer.style.left = _layer.style.top = _layer.style.right = _layer.style.bottom = 0;
            _status = new Label { name = "coverage-map-status", pickingMode = PickingMode.Ignore };
            _status.style.position = Position.Absolute;
            _status.style.left = 8;
            _status.style.bottom = 8;
            _status.style.color = Color.white;
            _status.style.backgroundColor = new Color(0.035f, 0.055f, 0.075f, 0.9f);
            _layer.Add(_status);
            _canvas.Add(_layer);
            _canvas.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        /// <summary>Number of valid authored polygons rendered in the current bind.</summary>
        public int PolygonCount { get; private set; }

        /// <summary>Number of pooled line elements active in the current bind.</summary>
        public int SegmentCount { get; private set; }

        /// <summary>Current validation and truncation text.</summary>
        public string StatusText => _status.text;

        /// <summary>Binds a current projection. Passing false hides all coverage while history is shown.</summary>
        public void Bind(CoordinationSnapshot snapshot, bool visible)
        {
            if (ReferenceEquals(_snapshot, snapshot) && _visible == visible) return;
            _snapshot = snapshot ?? CoordinationSnapshot.Empty;
            _visible = visible;
            _layer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            Rebuild();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt) => Rebuild();

        private void Rebuild()
        {
            PolygonCount = 0;
            SegmentCount = 0;
            HidePool();
            if (!_visible || _snapshot == null)
            {
                _status.text = _visible ? "Coverage: no frame" : "Coverage hidden during history inspection";
                return;
            }

            var width = _canvas.resolvedStyle.width;
            var height = _canvas.resolvedStyle.height;
            var drawable = float.IsFinite(width) && float.IsFinite(height) && width > 0 && height > 0;
            var invalid = 0;
            var truncatedPolygons = 0;
            var truncatedSegments = false;
            var plannedSegments = 0;
            foreach (var group in _snapshot.Groups)
            {
                foreach (var effect in group.Effects)
                {
                    var coverage = effect.Coverage;
                    if (coverage.Status == CoverageStatus.Unknown || coverage.Geometry == null) continue;
                    if (!IsValid(coverage.Geometry))
                    {
                        invalid++;
                        continue;
                    }
                    if (PolygonCount >= MaxPolygons)
                    {
                        truncatedPolygons++;
                        continue;
                    }

                    var required = RequiredSegments(coverage);
                    if (plannedSegments + required > MaxSegments)
                    {
                        truncatedSegments = true;
                        continue;
                    }

                    var polygonIndex = PolygonCount++;
                    plannedSegments += required;
                    if (drawable) RenderPolygon(coverage, polygonIndex, width, height);
                }
            }

            _status.text = (PolygonCount == 0 ? "Coverage: no usable authored spatial evidence" : $"Coverage: {PolygonCount} polygons | {SegmentCount} segments") +
                (invalid > 0 ? $" | INVALID {invalid} (finite [0..1] coordinates and source required)" : string.Empty) +
                (truncatedPolygons > 0 ? $" | TRUNCATED polygons {truncatedPolygons} (cap {MaxPolygons})" : string.Empty) +
                (truncatedSegments ? $" | TRUNCATED segments (cap {MaxSegments})" : string.Empty) +
                (!drawable && PolygonCount > 0 ? " | awaiting map geometry" : string.Empty);
        }

        private void RenderPolygon(CoverageAssessment coverage, int polygonIndex, float width, float height)
        {
            var points = coverage.Geometry!.Boundary;
            var gap = coverage.Status == CoverageStatus.Gap;
            for (var i = 0; i < points.Count; i++)
            {
                var a = new Vector2((float)points[i].NormalizedX * width, (float)points[i].NormalizedY * height);
                var bPoint = points[(i + 1) % points.Count];
                var b = new Vector2((float)bPoint.NormalizedX * width, (float)bPoint.NormalizedY * height);
                if (gap)
                {
                    for (var dash = 0; dash < 4; dash++)
                        ApplySegment(a + (b - a) * (dash / 4f), a + (b - a) * ((dash + 0.55f) / 4f), new Color(1f, 0.45f, 0.22f));
                }
                else ApplySegment(a, b, new Color(0.2f, 0.85f, 0.55f));
            }

            var label = GetLabel(polygonIndex);
            label.text = $"{(gap ? "GAP" : "COVERED")} — {coverage.Label}";
            label.tooltip = $"{coverage.Geometry.SourceRef} | {coverage.ReasonCode ?? "authored coverage fact"}";
            label.style.color = gap ? new Color(1f, 0.65f, 0.35f) : new Color(0.45f, 1f, 0.7f);
            label.style.left = (float)points[0].NormalizedX * width;
            label.style.top = (float)points[0].NormalizedY * height;
            label.style.display = DisplayStyle.Flex;
        }

        private void ApplySegment(Vector2 from, Vector2 to, Color color)
        {
            var line = GetSegment(SegmentCount++);
            var delta = to - from;
            line.style.left = from.x;
            line.style.top = from.y;
            line.style.width = delta.magnitude;
            line.style.rotate = new Rotate(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            line.style.backgroundColor = color;
            line.style.display = DisplayStyle.Flex;
        }

        private VisualElement GetSegment(int index)
        {
            if (index < _segments.Count) return _segments[index];
            var line = new VisualElement { name = "coverage-segment", pickingMode = PickingMode.Ignore };
            line.style.position = Position.Absolute;
            line.style.height = 2;
            line.style.transformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(50), 0);
            _layer.Insert(_layer.childCount - 1, line);
            _segments.Add(line);
            return line;
        }

        private Label GetLabel(int index)
        {
            if (index < _labels.Count) return _labels[index];
            var label = new Label { name = "coverage-label", pickingMode = PickingMode.Ignore };
            label.style.position = Position.Absolute;
            label.style.backgroundColor = new Color(0.035f, 0.055f, 0.075f, 0.9f);
            _layer.Add(label);
            _labels.Add(label);
            return label;
        }

        private void HidePool()
        {
            foreach (var line in _segments) line.style.display = DisplayStyle.None;
            foreach (var label in _labels) label.style.display = DisplayStyle.None;
        }

        private static int RequiredSegments(CoverageAssessment coverage) =>
            coverage.Geometry!.Boundary.Count * (coverage.Status == CoverageStatus.Gap ? 4 : 1);

        private static bool IsValid(CoverageAreaGeometry geometry)
        {
            if (string.IsNullOrWhiteSpace(geometry.SourceRef) || geometry.Boundary == null || geometry.Boundary.Count < 3) return false;
            foreach (var point in geometry.Boundary)
                if (!double.IsFinite(point.NormalizedX) || !double.IsFinite(point.NormalizedY) ||
                    point.NormalizedX < 0 || point.NormalizedX > 1 || point.NormalizedY < 0 || point.NormalizedY > 1) return false;
            return true;
        }

        /// <summary>Unregisters layout callbacks and removes all owned visual elements.</summary>
        public void Dispose()
        {
            _canvas.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            _layer.RemoveFromHierarchy();
        }
    }
}
#endif
