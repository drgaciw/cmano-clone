using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Delegation.UnityAdapter.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Authoring;

/// <summary>
/// MapAuthoringSurface + SelectionInspectorModel (Phase 2.1 Task 4):
/// rebuild glyphs, tentative cancel/commit, invalid geometry still commits, selection inspector.
/// </summary>
public sealed class MapAuthoringSurfaceTests
{
    [Test]
    public void RebuildFromDocument_creates_glyph_per_orbat_unit()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-map-rebuild-{Guid.NewGuid():N}.json");
        try
        {
            var editor = ScenarioDocumentEditor.CreateNew();
            editor.UpsertOrbatUnit(new ScenarioOrbatUnitDto
            {
                Id = "u2",
                SideId = "blue",
                PlatformId = "ffg",
                Lat = 57.1,
                Lon = 20.1,
            });
            editor.UpsertOrbatUnit(new ScenarioOrbatUnitDto
            {
                Id = "u1",
                SideId = "red",
                PlatformId = "ddg",
                Lat = 56.9,
                Lon = 19.9,
            });
            editor.CommitMutation();
            editor.Save(path);

            using var session = ScenarioAuthoringSession.Open(path);
            var surface = new MapAuthoringSurface(session);
            surface.RebuildFromDocument();

            Assert.That(surface.Units.Count, Is.EqualTo(2));
            Assert.That(surface.Units[0].UnitId, Is.EqualTo("u1")); // ordinal order
            Assert.That(surface.Units[1].UnitId, Is.EqualTo("u2"));
            Assert.That(surface.Units[0].SideId, Is.EqualTo("red"));
            Assert.That(surface.Units[0].PlatformId, Is.EqualTo("ddg"));
            Assert.That(surface.Units[0].Lat, Is.EqualTo(56.9).Within(1e-9));
            Assert.That(surface.Units[0].Lon, Is.EqualTo(19.9).Within(1e-9));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Test]
    public void BeginPlaceUnit_then_Cancel_leaves_document_unchanged()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-map-cancel-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            var surface = new MapAuthoringSurface(session);
            surface.RebuildFromDocument();
            var versionBefore = session.EditVersion;

            surface.BeginPlaceUnit(new ScenarioOrbatUnitDto
            {
                Id = "u-new",
                SideId = "blue",
                PlatformId = "ffg",
                Lat = 57,
                Lon = 20,
            });
            Assert.That(surface.TentativeUnit, Is.Not.Null);
            surface.CancelGesture();

            Assert.That(surface.TentativeUnit, Is.Null);
            Assert.That(session.EditVersion, Is.EqualTo(versionBefore));
            Assert.That(session.IsDirty, Is.False);
            surface.RebuildFromDocument();
            Assert.That(surface.Units, Is.Empty);

            var reloaded = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(reloaded.Orbat?.Units ?? Array.Empty<ScenarioOrbatUnitDto>(), Is.Empty);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Test]
    public void BeginPlaceUnit_then_CommitGesture_calls_session_bus_and_adds_glyph()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-map-commit-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            var surface = new MapAuthoringSurface(session);
            surface.RebuildFromDocument();
            Assert.That(surface.Units, Is.Empty);

            surface.BeginPlaceUnit(new ScenarioOrbatUnitDto
            {
                Id = "u1",
                SideId = "blue",
                PlatformId = "ffg",
                Lat = 57,
                Lon = 20,
            });
            var result = surface.CommitPlaceUnit(save: true);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Ok, Is.True);
            Assert.That(result.EditVersion, Is.EqualTo(2));
            Assert.That(surface.TentativeUnit, Is.Null);
            Assert.That(surface.Units.Count, Is.EqualTo(1));
            Assert.That(surface.Units[0].UnitId, Is.EqualTo("u1"));
            Assert.That(surface.Units[0].Lat, Is.EqualTo(57).Within(1e-9));
            Assert.That(surface.Units[0].Lon, Is.EqualTo(20).Within(1e-9));

            var reloaded = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(reloaded.Orbat!.Units.Count, Is.EqualTo(1));
            Assert.That(reloaded.Orbat.Units[0].Id, Is.EqualTo("u1"));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Test]
    public void DrawPolygon_invalid_until_three_vertices_sets_InvalidGeometry_flag()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-map-poly-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            var surface = new MapAuthoringSurface(session);

            // Commit a 2-vertex polygon — invalid geometry still commits (design §10)
            surface.BeginDrawReferencePoint(new ScenarioReferencePointDto
            {
                Id = "rp-poly",
                Type = "polygon",
                Geometry =
                [
                    new ScenarioWaypointDto { Lat = 57, Lon = 20 },
                    new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                ],
            });
            var result = surface.CommitReferencePoint(save: true);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Ok, Is.True);
            Assert.That(surface.ReferencePoints.Count, Is.EqualTo(1));
            Assert.That(surface.ReferencePoints[0].Id, Is.EqualTo("rp-poly"));
            Assert.That(surface.ReferencePoints[0].IsGeometryValid, Is.False);
            Assert.That(surface.ReferencePoints[0].InvalidReason, Does.Contain("polygon").IgnoreCase);

            // Three vertices → valid after rebuild
            surface.BeginDrawReferencePoint(new ScenarioReferencePointDto
            {
                Id = "rp-poly",
                Type = "polygon",
                Geometry =
                [
                    new ScenarioWaypointDto { Lat = 57, Lon = 20 },
                    new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                    new ScenarioWaypointDto { Lat = 57.2, Lon = 20.2 },
                ],
            });
            var okResult = surface.CommitReferencePoint(save: true);
            Assert.That(okResult!.Ok, Is.True);
            Assert.That(surface.ReferencePoints[0].IsGeometryValid, Is.True);
            Assert.That(surface.ReferencePoints[0].InvalidReason, Is.Null);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Test]
    public void SelectUnit_populates_SelectionInspectorModel()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-map-sel-{Guid.NewGuid():N}.json");
        try
        {
            var editor = ScenarioDocumentEditor.CreateNew();
            editor.UpsertOrbatUnit(new ScenarioOrbatUnitDto
            {
                Id = "u1",
                SideId = "blue",
                PlatformId = "ffg",
                Lat = 57.5,
                Lon = 20.25,
            });
            editor.CommitMutation();
            editor.Save(path);

            using var session = ScenarioAuthoringSession.Open(path);
            var surface = new MapAuthoringSurface(session);
            surface.RebuildFromDocument();
            surface.SelectUnit("u1");

            Assert.That(surface.Selection.SelectedUnitId, Is.EqualTo("u1"));
            Assert.That(surface.Selection.SelectedReferencePointId, Is.Null);
            Assert.That(surface.Selection.SummaryLine, Does.Contain("u1"));
            Assert.That(surface.Selection.SummaryLine, Does.Contain("blue"));
            Assert.That(surface.Selection.SummaryLine, Does.Contain("ffg"));
            Assert.That(surface.Selection.SummaryLine, Does.Contain("57.500"));
            Assert.That(surface.Selection.SummaryLine, Does.Contain("20.250"));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
