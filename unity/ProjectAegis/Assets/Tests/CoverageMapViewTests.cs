#if UNITY_5_3_OR_NEWER
using System;
using System.Linq;
using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;
using ProjectAegis.Unity.Runtime;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Tests
{
    public sealed class CoverageMapViewTests
    {
        [Test]
        public void Bind_hides_during_history_and_dispose_removes_owned_layer()
        {
            var canvas = new VisualElement();
            var view = new CoverageMapView(canvas);
            view.Bind(Snapshot(Assessment(CoverageStatus.Covered)), false);

            Assert.That(canvas.Q("coverage-map-layer").style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(view.StatusText, Does.Contain("hidden during history"));
            view.Dispose();
            Assert.That(canvas.Q("coverage-map-layer"), Is.Null);
        }

        [Test]
        public void Bind_accepts_only_authored_finite_normalized_geometry_with_source()
        {
            var canvas = new VisualElement();
            var view = new CoverageMapView(canvas);
            var invalid = new CoverageAssessment(CoverageStatus.Gap, "bad", "Bad",
                new CoverageAreaGeometry(new[] { new CoverageAreaPoint(double.NaN, 0), new CoverageAreaPoint(1.1, 0), new CoverageAreaPoint(0, 1) }, ""), "BAD");

            view.Bind(Snapshot(invalid), true);

            Assert.That(view.PolygonCount, Is.Zero);
            Assert.That(view.StatusText, Does.Contain("INVALID 1"));
            Assert.That(view.StatusText, Does.Contain("finite [0..1] coordinates and source required"));
            view.Dispose();
        }

        [Test]
        public void Bind_caps_polygons_and_reports_truncation_without_inferred_radius()
        {
            var canvas = new VisualElement();
            var view = new CoverageMapView(canvas);
            var effects = Enumerable.Range(0, CoverageMapView.MaxPolygons + 7)
                .Select(i => Effect(Assessment(i % 2 == 0 ? CoverageStatus.Covered : CoverageStatus.Gap, i)))
                .ToArray();

            view.Bind(new CoordinationSnapshot(new[] { new CoordinationGroupSnapshot(null!, null!, effects, Array.Empty<CoordinationGap>()) }), true);

            Assert.That(view.PolygonCount, Is.EqualTo(CoverageMapView.MaxPolygons));
            Assert.That(view.StatusText, Does.Contain("TRUNCATED polygons 7"));
            Assert.That(view.StatusText, Does.Not.Contain("radius"));
            view.Dispose();
        }

        [Test]
        public void Oversized_boundary_is_omitted_with_explicit_segment_budget_disclosure()
        {
            var points = Enumerable.Range(0, 600).Select(i => new CoverageAreaPoint(
                .5 + .4 * Math.Cos(i * Math.PI / 300), .5 + .4 * Math.Sin(i * Math.PI / 300))).ToArray();
            var assessment = new CoverageAssessment(CoverageStatus.Gap, "large", "Large sector",
                new CoverageAreaGeometry(points, "fixture:large-sector"), "LOST");
            using var view = new CoverageMapView(new VisualElement());
            view.Bind(Snapshot(assessment), true);
            Assert.That(view.PolygonCount, Is.Zero);
            Assert.That(view.SegmentCount, Is.Zero);
            Assert.That(view.StatusText, Does.Contain("TRUNCATED segments"));
        }

        private static CoordinationSnapshot Snapshot(CoverageAssessment coverage) =>
            new(new[] { new CoordinationGroupSnapshot(null!, null!, new[] { Effect(coverage) }, Array.Empty<CoordinationGap>()) });

        private static CoordinationEffect Effect(CoverageAssessment coverage) =>
            new("g1", "u1", coverage.CoverageId, null, CoordinationEffectState.Available, coverage);

        private static CoverageAssessment Assessment(CoverageStatus status, int id = 1) => new(
            status, $"sector-{id}", $"Sector {id}",
            new CoverageAreaGeometry(new[] { new CoverageAreaPoint(0.1, 0.1), new CoverageAreaPoint(0.9, 0.1), new CoverageAreaPoint(0.5, 0.8) }, $"scenario:coverage/{id}"),
            status == CoverageStatus.Gap ? "CONTRIBUTOR_UNAVAILABLE" : null);
    }
}
#endif
