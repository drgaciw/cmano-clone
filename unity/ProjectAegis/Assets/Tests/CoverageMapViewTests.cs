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
                new CoverageAreaGeometry([new(double.NaN, 0), new(1.1, 0), new(0, 1)], ""), "BAD");

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

            view.Bind(new CoordinationSnapshot([new CoordinationGroupSnapshot(null!, null!, effects, [])]), true);

            Assert.That(view.PolygonCount, Is.EqualTo(CoverageMapView.MaxPolygons));
            Assert.That(view.StatusText, Does.Contain("TRUNCATED polygons 7"));
            Assert.That(view.StatusText, Does.Not.Contain("radius"));
            view.Dispose();
        }

        [Test]
        public void Source_declares_gap_dashes_covered_solids_geometry_scaling_and_segment_cap()
        {
            var source = System.IO.File.ReadAllText(System.IO.Path.Combine(
                TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "ProjectAegis", "Assets", "Scripts", "Runtime", "CoverageMapView.cs"));
            Assert.Multiple(() =>
            {
                Assert.That(source, Does.Contain("coverage.Status == CoverageStatus.Gap ? 4 : 1"));
                Assert.That(source, Does.Contain("gap ? \"GAP\" : \"COVERED\""));
                Assert.That(source, Does.Contain("GeometryChangedEvent"));
                Assert.That(source, Does.Contain("MaxSegments = 2048"));
                Assert.That(source, Does.Not.Contain("radius"));
            });
        }

        private static CoordinationSnapshot Snapshot(CoverageAssessment coverage) =>
            new([new CoordinationGroupSnapshot(null!, null!, [Effect(coverage)], [])]);

        private static CoordinationEffect Effect(CoverageAssessment coverage) =>
            new("g1", "u1", coverage.CoverageId, null, CoordinationEffectState.Available, coverage);

        private static CoverageAssessment Assessment(CoverageStatus status, int id = 1) => new(
            status, $"sector-{id}", $"Sector {id}",
            new CoverageAreaGeometry([new(0.1, 0.1), new(0.9, 0.1), new(0.5, 0.8)], $"scenario:coverage/{id}"),
            status == CoverageStatus.Gap ? "CONTRIBUTOR_UNAVAILABLE" : null);
    }
}
#endif
