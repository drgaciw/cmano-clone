namespace ProjectAegis.Data.Scenario.Authoring;

using System.Globalization;
using System.Text;
using System.Text.Json;

/// <summary>Deterministic scenario.json writer (GDD §3.2 AC-6: stable keys, fixed numeric format, LF).</summary>
public static class ScenarioStableJsonWriter
{
    public static string Serialize(ScenarioDocumentDto document)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            WriteDocument(writer, document);
        }

        return Encoding.UTF8.GetString(stream.ToArray()).Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    public static void WriteToFile(ScenarioDocumentDto document, string path)
    {
        var json = Serialize(document);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(path, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static void WriteDocument(Utf8JsonWriter w, ScenarioDocumentDto doc)
    {
        w.WriteStartObject();
        w.WritePropertyName("metadata");
        WriteMetadata(w, doc.Metadata);

        if (doc.Features != null)
        {
            w.WritePropertyName("features");
            WriteFeatures(w, doc.Features);
        }

        if (doc.Sides.Count > 0)
        {
            w.WritePropertyName("sides");
            WriteSides(w, doc.Sides);
        }

        if (doc.Orbat != null)
        {
            w.WritePropertyName("orbat");
            WriteOrbat(w, doc.Orbat);
        }

        if (doc.ReferencePoints.Count > 0)
        {
            w.WritePropertyName("referencePoints");
            WriteReferencePoints(w, doc.ReferencePoints);
        }

        w.WritePropertyName("missions");
        WriteMissions(w, doc.Missions);

        if (doc.OperationsTimeline.Count > 0)
        {
            w.WritePropertyName("operationsTimeline");
            WriteOperationsTimeline(w, doc.OperationsTimeline);
        }

        if (doc.Events.Count > 0)
        {
            w.WritePropertyName("events");
            WriteEvents(w, doc.Events);
        }

        if (doc.Variables is { Count: > 0 })
        {
            w.WritePropertyName("variables");
            WriteVariables(w, doc.Variables);
        }

        if (doc.EditorState != null)
        {
            w.WritePropertyName("editorState");
            WriteEditorState(w, doc.EditorState);
        }

        w.WriteEndObject();
    }

    private static void WriteMetadata(Utf8JsonWriter w, ScenarioMetadataDto m)
    {
        w.WriteStartObject();
        WriteStringIfPresent(w, "title", m.Title);
        WriteStringIfPresent(w, "description", m.Description);
        WriteStringIfPresent(w, "author", m.Author);
        w.WriteNumber("schemaVersion", m.SchemaVersion);
        WriteStringIfPresent(w, "dbRef", m.DbRef);
        WriteStringIfPresent(w, "dbSnapshotId", m.DbSnapshotId);
        w.WriteNumber("editVersion", m.EditVersion);
        w.WriteNumber("seed", m.Seed);
        WriteStringIfPresent(w, "policyId", m.PolicyId);
        w.WriteNumber("maxTechnologyLevel", m.MaxTechnologyLevel);

        if (m.UnitReadiness is { Count: > 0 })
        {
            w.WritePropertyName("unitReadiness");
            w.WriteStartObject();
            foreach (var key in m.UnitReadiness.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                w.WritePropertyName(key);
                w.WriteStartObject();
                w.WriteBoolean("readyForLaunch", m.UnitReadiness[key].ReadyForLaunch);
                w.WriteEndObject();
            }

            w.WriteEndObject();
        }

        if (m.NearFutureUnits is { Count: > 0 })
        {
            w.WritePropertyName("nearFutureUnits");
            w.WriteStartArray();
            foreach (var unit in m.NearFutureUnits.OrderBy(u => u.UnitId, StringComparer.Ordinal))
            {
                w.WriteStartObject();
                w.WriteString("unitId", unit.UnitId);
                w.WriteString("archetypeId", unit.ArchetypeId);
                w.WriteEndObject();
            }

            w.WriteEndArray();
        }

        w.WriteEndObject();
    }

    private static void WriteFeatures(Utf8JsonWriter w, ScenarioFeaturesDto f)
    {
        w.WriteStartObject();
        w.WriteBoolean("realismMagazines", f.RealismMagazines);
        w.WriteNumber("maxTimeCompression", f.MaxTimeCompression);
        w.WriteEndObject();
    }

    private static void WriteSides(Utf8JsonWriter w, IReadOnlyList<ScenarioSideDto> sides)
    {
        w.WriteStartArray();
        foreach (var side in sides.OrderBy(s => s.Id, StringComparer.Ordinal))
        {
            w.WriteStartObject();
            w.WriteString("id", side.Id);
            w.WriteString("name", side.Name);
            WriteStringIfPresent(w, "defaultRoe", side.DefaultRoe);
            WriteStringIfPresent(w, "defaultEmcon", side.DefaultEmcon);
            if (side.Postures.Count > 0)
            {
                w.WritePropertyName("postures");
                w.WriteStartArray();
                foreach (var posture in side.Postures.OrderBy(p => p, StringComparer.Ordinal))
                {
                    w.WriteStringValue(posture);
                }

                w.WriteEndArray();
            }

            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    private static void WriteOrbat(Utf8JsonWriter w, ScenarioOrbatDto orbat)
    {
        w.WriteStartObject();
        w.WritePropertyName("units");
        w.WriteStartArray();
        foreach (var unit in orbat.Units.OrderBy(u => u.Id, StringComparer.Ordinal))
        {
            w.WriteStartObject();
            w.WriteString("id", unit.Id);
            w.WriteString("sideId", unit.SideId);
            w.WriteString("platformId", unit.PlatformId);
            WriteNumber(w, "lat", unit.Lat);
            WriteNumber(w, "lon", unit.Lon);
            WriteStringIfPresent(w, "parentUnitId", unit.ParentUnitId);
            WriteStringIfPresent(w, "roeOverride", unit.RoeOverride);
            WriteStringIfPresent(w, "emconOverride", unit.EmconOverride);
            w.WriteEndObject();
        }

        w.WriteEndArray();

        w.WritePropertyName("bases");
        w.WriteStartArray();
        foreach (var b in orbat.Bases.OrderBy(b => b.Id, StringComparer.Ordinal))
        {
            w.WriteStartObject();
            w.WriteString("id", b.Id);
            w.WriteString("sideId", b.SideId);
            WriteNumber(w, "lat", b.Lat);
            WriteNumber(w, "lon", b.Lon);
            w.WriteEndObject();
        }

        w.WriteEndArray();
        w.WriteEndObject();
    }

    private static void WriteReferencePoints(Utf8JsonWriter w, IReadOnlyList<ScenarioReferencePointDto> points)
    {
        w.WriteStartArray();
        foreach (var rp in points.OrderBy(p => p.Id, StringComparer.Ordinal))
        {
            w.WriteStartObject();
            w.WriteString("id", rp.Id);
            w.WriteString("type", rp.Type);
            w.WritePropertyName("geometry");
            WriteWaypoints(w, rp.Geometry);
            if (rp.RadiusNm.HasValue)
            {
                WriteNumber(w, "radiusNm", rp.RadiusNm.Value);
            }

            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    private static void WriteMissions(Utf8JsonWriter w, IReadOnlyList<ScenarioMissionDto> missions)
    {
        w.WriteStartArray();
        foreach (var mission in missions.OrderBy(m => m.Id, StringComparer.Ordinal))
        {
            w.WriteStartObject();
            w.WriteString("id", mission.Id);
            w.WriteString("type", mission.Type);
            w.WritePropertyName("assignedUnitIds");
            WriteStringArray(w, mission.AssignedUnitIds);
            if (mission.TargetIds.Count > 0)
            {
                w.WritePropertyName("targetIds");
                WriteStringArray(w, mission.TargetIds);
            }

            WriteStringIfPresent(w, "ferryDestinationBaseId", mission.FerryDestinationBaseId);
            WriteStringIfPresent(w, "supportRole", mission.SupportRole);
            if (mission.PatrolZone.Count > 0)
            {
                w.WritePropertyName("patrolZone");
                WriteWaypoints(w, mission.PatrolZone);
            }

            if (mission.StationGeometry is { Count: > 0 })
            {
                w.WritePropertyName("stationGeometry");
                WriteWaypoints(w, mission.StationGeometry);
            }

            WriteStringIfPresent(w, "roeOverride", mission.RoeOverride);
            WriteStringIfPresent(w, "emconOverride", mission.EmconOverride);
            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    private static void WriteOperationsTimeline(Utf8JsonWriter w, IReadOnlyList<ScenarioOperationTimelineEntryDto> entries)
    {
        w.WriteStartArray();
        foreach (var entry in entries.OrderBy(e => e.ActivateAtTick).ThenBy(e => e.MissionId, StringComparer.Ordinal))
        {
            w.WriteStartObject();
            w.WriteString("missionId", entry.MissionId);
            w.WriteNumber("activateAtTick", entry.ActivateAtTick);
            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    private static void WriteEvents(Utf8JsonWriter w, IReadOnlyList<ScenarioEventDto> events)
    {
        w.WriteStartArray();
        foreach (var evt in events.OrderBy(e => e.Id, StringComparer.Ordinal))
        {
            w.WriteStartObject();
            w.WriteString("id", evt.Id);
            w.WriteNumber("priority", evt.Priority);
            w.WritePropertyName("trigger");
            w.WriteStartObject();
            w.WriteString("type", evt.Trigger.Type);
            if (evt.Trigger.AtTick.HasValue)
            {
                w.WriteNumber("atTick", evt.Trigger.AtTick.Value);
            }

            w.WriteEndObject();

            if (evt.Conditions.Count > 0)
            {
                w.WritePropertyName("conditions");
                w.WriteStartArray();
                foreach (var cond in evt.Conditions)
                {
                    w.WriteStartObject();
                    w.WriteString("type", cond.Type);
                    WriteStringIfPresent(w, "unitId", cond.UnitId);
                    WriteStringIfPresent(w, "zoneId", cond.ZoneId);
                    w.WriteEndObject();
                }

                w.WriteEndArray();
            }

            if (evt.Actions.Count > 0)
            {
                w.WritePropertyName("actions");
                w.WriteStartArray();
                foreach (var action in evt.Actions)
                {
                    w.WriteStartObject();
                    w.WriteString("type", action.Type);
                    WriteStringIfPresent(w, "missionId", action.MissionId);
                    WriteStringIfPresent(w, "unitId", action.UnitId);
                    if (action.Lat.HasValue)
                    {
                        WriteNumber(w, "lat", action.Lat.Value);
                    }

                    if (action.Lon.HasValue)
                    {
                        WriteNumber(w, "lon", action.Lon.Value);
                    }

                    w.WriteEndObject();
                }

                w.WriteEndArray();
            }

            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    private static void WriteVariables(Utf8JsonWriter w, Dictionary<string, string> variables)
    {
        w.WriteStartObject();
        foreach (var key in variables.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            w.WriteString(key, variables[key]);
        }

        w.WriteEndObject();
    }

    private static void WriteEditorState(Utf8JsonWriter w, ScenarioEditorStateDto state)
    {
        w.WriteStartObject();
        WriteNumber(w, "cameraLat", state.CameraLat);
        WriteNumber(w, "cameraLon", state.CameraLon);
        WriteNumber(w, "cameraZoom", state.CameraZoom);
        w.WriteBoolean("layersVisible", state.LayersVisible);
        w.WriteEndObject();
    }

    private static void WriteWaypoints(Utf8JsonWriter w, IReadOnlyList<ScenarioWaypointDto> waypoints)
    {
        w.WriteStartArray();
        foreach (var wp in waypoints)
        {
            w.WriteStartObject();
            WriteNumber(w, "lat", wp.Lat);
            WriteNumber(w, "lon", wp.Lon);
            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    private static void WriteStringArray(Utf8JsonWriter w, IReadOnlyList<string> values)
    {
        w.WriteStartArray();
        foreach (var value in values.OrderBy(v => v, StringComparer.Ordinal))
        {
            w.WriteStringValue(value);
        }

        w.WriteEndArray();
    }

    private static void WriteStringIfPresent(Utf8JsonWriter w, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            w.WriteString(name, value);
        }
    }

    private static void WriteNumber(Utf8JsonWriter w, string name, double value)
    {
        w.WritePropertyName(name);
        w.WriteRawValue(value.ToString("F6", CultureInfo.InvariantCulture));
    }
}
