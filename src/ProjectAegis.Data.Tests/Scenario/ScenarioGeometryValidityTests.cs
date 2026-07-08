using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>
/// Map-first invalid-mark contract for reference-point geometry (AME-4.3 / P2.1).
/// Pure helper — not a Validation Engine rule.
/// </summary>
public sealed class ScenarioGeometryValidityTests
{
    [Fact]
    public void Point_with_one_vertex_is_valid()
    {
        var rp = new ScenarioReferencePointDto
        {
            Id = "rp-point",
            Type = "point",
            Geometry =
            [
                new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
            ],
        };

        var ok = ScenarioGeometryValidity.IsValid(rp, out var reason);

        Assert.True(ok);
        Assert.Null(reason);
    }

    [Fact]
    public void Polygon_with_fewer_than_three_vertices_is_invalid()
    {
        var rp = new ScenarioReferencePointDto
        {
            Id = "rp-poly",
            Type = "polygon",
            Geometry =
            [
                new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
                new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
            ],
        };

        var ok = ScenarioGeometryValidity.IsValid(rp, out var reason);

        Assert.False(ok);
        Assert.NotNull(reason);
        Assert.Contains("polygon", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Circle_without_positive_radius_is_invalid()
    {
        var rp = new ScenarioReferencePointDto
        {
            Id = "rp-circle",
            Type = "circle",
            Geometry =
            [
                new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
            ],
            RadiusNm = null,
        };

        var ok = ScenarioGeometryValidity.IsValid(rp, out var reason);

        Assert.False(ok);
        Assert.NotNull(reason);
        Assert.Contains("circle", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Circle_with_center_and_positive_radius_is_valid()
    {
        var rp = new ScenarioReferencePointDto
        {
            Id = "rp-circle",
            Type = "circle",
            Geometry =
            [
                new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
            ],
            RadiusNm = 12.5,
        };

        var ok = ScenarioGeometryValidity.IsValid(rp, out var reason);

        Assert.True(ok);
        Assert.Null(reason);
    }

    [Fact]
    public void Line_with_fewer_than_two_vertices_is_invalid()
    {
        var rp = new ScenarioReferencePointDto
        {
            Id = "rp-line",
            Type = "line",
            Geometry =
            [
                new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
            ],
        };

        var ok = ScenarioGeometryValidity.IsValid(rp, out var reason);

        Assert.False(ok);
        Assert.NotNull(reason);
        Assert.Contains("line", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Null_reference_point_is_invalid()
    {
        var ok = ScenarioGeometryValidity.IsValid(null!, out var reason);

        Assert.False(ok);
        Assert.NotNull(reason);
    }

    [Fact]
    public void Unknown_type_is_invalid()
    {
        var rp = new ScenarioReferencePointDto
        {
            Id = "rp-unknown",
            Type = "hexagon",
            Geometry =
            [
                new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
            ],
        };

        var ok = ScenarioGeometryValidity.IsValid(rp, out var reason);

        Assert.False(ok);
        Assert.NotNull(reason);
    }

    [Fact]
    public void Corridor_requires_at_least_two_vertices()
    {
        var invalid = new ScenarioReferencePointDto
        {
            Id = "rp-corridor",
            Type = "corridor",
            Geometry =
            [
                new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
            ],
        };
        var valid = new ScenarioReferencePointDto
        {
            Id = "rp-corridor",
            Type = "corridor",
            Geometry =
            [
                new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
                new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
            ],
        };

        Assert.False(ScenarioGeometryValidity.IsValid(invalid, out var badReason));
        Assert.NotNull(badReason);
        Assert.True(ScenarioGeometryValidity.IsValid(valid, out var goodReason));
        Assert.Null(goodReason);
    }

    [Fact]
    public void Polygon_with_three_vertices_is_valid()
    {
        var rp = new ScenarioReferencePointDto
        {
            Id = "rp-poly",
            Type = "polygon",
            Geometry =
            [
                new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
                new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                new ScenarioWaypointDto { Lat = 57.2, Lon = 20.0 },
            ],
        };

        var ok = ScenarioGeometryValidity.IsValid(rp, out var reason);

        Assert.True(ok);
        Assert.Null(reason);
    }
}
