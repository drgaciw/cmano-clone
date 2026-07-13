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

    public static double GetDoubleFlag(string[] args, string name, double defaultValue)
    {
        var raw = GetFlag(args, name);
        return double.TryParse(raw, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : defaultValue;
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

    /// <summary>
    /// Returns the true positional (non-flag) tokens in <paramref name="args"/>, skipping both each
    /// flag token in <paramref name="valueFlags"/> AND the value that immediately follows it. Without
    /// this, a naive "drop tokens starting with '-'" filter leaves a flag's *value* behind (e.g. the
    /// path argument to "--db") and it gets misread as if it were a positional argument, shifting every
    /// subsequent positional index by one.
    /// </summary>
    public static IReadOnlyList<string> GetPositional(string[] args, params string[] valueFlags)
    {
        var flagSet = new HashSet<string>(valueFlags, StringComparer.Ordinal);
        var positional = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("-", StringComparison.Ordinal))
            {
                positional.Add(arg);
                continue;
            }

            if (flagSet.Contains(arg) && i + 1 < args.Length)
            {
                i++; // consume this flag's value so it isn't counted as positional
            }
        }

        return positional;
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