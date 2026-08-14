// Globe overlay visual bind (DRG-161 / S121-CESIUM) — UI Toolkit only, no Cesium package.
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    /// <summary>
    /// Draws projected envelope rings and datalink edges on the product globe host.
    /// Uses headless <see cref="GlobeOverlayScreenProjection"/> — presentation-only.
    /// </summary>
    internal sealed class GlobeOverlayVisualLayer : VisualElement
    {
        private const string LayerName = "globe-overlay-layer";

        private IReadOnlyList<GlobeEnvelopeRingMarker> _rings = System.Array.Empty<GlobeEnvelopeRingMarker>();
        private IReadOnlyList<GlobeDatalinkEdgeMarker> _edges = System.Array.Empty<GlobeDatalinkEdgeMarker>();
        private GlobeCameraState _camera = GlobeViewProjection.DefaultBalticTheater().Camera;

        public GlobeOverlayVisualLayer()
        {
            name = LayerName;
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0;
            style.right = 0;
            style.top = 0;
            style.bottom = 0;
            generateVisualContent += OnGenerateVisualContent;
        }

        public void Bind(
            GlobeCameraState camera,
            IReadOnlyList<GlobeEnvelopeRingMarker> rings,
            IReadOnlyList<GlobeDatalinkEdgeMarker> edges)
        {
            _camera = camera ?? throw new System.ArgumentNullException(nameof(camera));
            _rings = rings ?? System.Array.Empty<GlobeEnvelopeRingMarker>();
            _edges = edges ?? System.Array.Empty<GlobeDatalinkEdgeMarker>();
            MarkDirtyRepaint();
        }

        /// <summary>Camera-only update — skips geometry rebind when sim data unchanged.</summary>
        public void BindCamera(GlobeCameraState camera)
        {
            _camera = camera ?? throw new System.ArgumentNullException(nameof(camera));
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            var painter = context.painter2D;
            if (painter == null)
            {
                return;
            }

            var width = contentRect.width;
            var height = contentRect.height;
            if (width <= 1f || height <= 1f)
            {
                return;
            }

            foreach (var edge in _edges)
            {
                if (edge is null)
                {
                    continue;
                }

                if (!GlobeOverlayScreenProjection.TryProject(
                        edge.FromLatitude,
                        edge.FromLongitude,
                        _camera,
                        out var from)
                    || !GlobeOverlayScreenProjection.TryProject(
                        edge.ToLatitude,
                        edge.ToLongitude,
                        _camera,
                        out var to))
                {
                    continue;
                }

                painter.strokeColor = ResolveEdgeColor(edge.Status);
                painter.lineWidth = 2f;
                painter.BeginPath();
                painter.MoveTo(ToLocal(from, width, height));
                painter.LineTo(ToLocal(to, width, height));
                painter.Stroke();
            }

            foreach (var ring in _rings)
            {
                if (ring is null || ring.Polyline.Count < 2)
                {
                    continue;
                }

                DrawPolyline(painter, ring.Polyline, width, height, ring.RingKind, ring.IsSelectedUnit);
            }
        }

        private void DrawPolyline(
            Painter2D painter,
            IReadOnlyList<(double Latitude, double Longitude)> polyline,
            float width,
            float height,
            string ringKind,
            bool isSelectedUnit)
        {
            var started = false;
            foreach (var vertex in polyline)
            {
                if (!GlobeOverlayScreenProjection.TryProject(
                        vertex.Latitude,
                        vertex.Longitude,
                        _camera,
                        out var point))
                {
                    started = false;
                    continue;
                }

                var local = ToLocal(point, width, height);
                if (!started)
                {
                    painter.strokeColor = ResolveRingColor(ringKind);
                    painter.lineWidth = isSelectedUnit ? 2.5f : 1.5f;
                    painter.BeginPath();
                    painter.MoveTo(local);
                    started = true;
                    continue;
                }

                painter.LineTo(local);
            }

            if (started)
            {
                painter.Stroke();
            }
        }

        private static Vector2 ToLocal(GlobeViewportPoint point, float width, float height) =>
            new((float)(point.X * width), (float)((1.0 - point.Y) * height));

        private static Color ResolveRingColor(string ringKind) =>
            string.Equals(ringKind, TacticalOverlayProjection.RingKindWeapon, System.StringComparison.Ordinal)
                ? new Color(1f, 0.45f, 0.2f, 0.85f)
                : new Color(0.35f, 0.75f, 1f, 0.85f);

        private static Color ResolveEdgeColor(string status) =>
            status switch
            {
                DatalinkPictureProjection.StatusDegraded => new Color(1f, 0.85f, 0.2f, 0.9f),
                DatalinkPictureProjection.StatusDown => new Color(0.9f, 0.25f, 0.25f, 0.9f),
                _ => new Color(0.4f, 0.95f, 0.55f, 0.9f),
            };
    }
}
#endif
