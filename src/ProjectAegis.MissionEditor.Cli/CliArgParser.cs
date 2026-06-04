namespace ProjectAegis.MissionEditor.Cli;

using ProjectAegis.Data.Scenario.Authoring;

public static class CliArgParser
{
    public static string? GetFlag(string[] args, string name)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == name && i + 1 < args.Length)
            {
                return args[++i];
            }
        }

        return null;
    }

    public static int GetIntFlag(string[] args, string name, int defaultValue)
    {
        var raw = GetFlag(args, name);
        return int.TryParse(raw, out var value) ? value : defaultValue;
    }

    public static ulong? GetULongFlag(string[] args, string name)
    {
        var raw = GetFlag(args, name);
        return ulong.TryParse(raw, out var value) ? value : null;
    }

    public static IReadOnlyList<string> GetRepeated(string[] args, string name)
    {
        var list = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == name && i + 1 < args.Length)
            {
                list.Add(args[++i]);
            }
        }

        return list;
    }

    public static IReadOnlyList<ScenarioWaypointDto> ParseWaypoints(IReadOnlyList<string> waypointArgs)
    {
        var zone = new List<ScenarioWaypointDto>();
        foreach (var wp in waypointArgs)
        {
            var parts = wp.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length != 2 ||
                !double.TryParse(parts[0], out var lat) ||
                !double.TryParse(parts[1], out var lon))
            {
                throw new FormatException($"Invalid waypoint '{wp}'. Expected lat,lon.");
            }

            zone.Add(new ScenarioWaypointDto { Lat = lat, Lon = lon });
        }

        return zone;
    }
}