namespace ProjectAegis.Sim.Scenario;

/// <summary>
/// Side map for Baltic headless OOB: legacy synthetic ids (u1/hostile-1 + UCAV pair)
/// plus scenario-scoped catalog platform registrations (gauntlet ORBAT).
/// </summary>
public static class BalticV3SideRegistry
{
    private static readonly object Gate = new();
    private static Dictionary<string, string>? _scenarioSides;

    /// <summary>Register a unit id as blue or red for the current scenario run.</summary>
    public static void RegisterScenarioSide(string unitId, string side)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return;
        }

        var normalized = string.Equals(side, "red", StringComparison.OrdinalIgnoreCase) ? "red" : "blue";
        lock (Gate)
        {
            _scenarioSides ??= new Dictionary<string, string>(StringComparer.Ordinal);
            _scenarioSides[unitId] = normalized;
        }
    }

    /// <summary>Clear scenario-scoped sides (call after each harness run).</summary>
    public static void ClearScenarioSides()
    {
        lock (Gate)
        {
            _scenarioSides = null;
        }
    }

    public static string? GetSideForUnit(string unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return null;
        }

        lock (Gate)
        {
            if (_scenarioSides != null
                && _scenarioSides.TryGetValue(unitId, out var scenarioSide))
            {
                return scenarioSide;
            }
        }

        return unitId switch
        {
            "u1" or "ucav-blue" => "blue",
            "hostile-1" or "ucav-red" => "red",
            _ => null,
        };
    }

    public static bool IsBlueForceUnit(string unitId) =>
        string.Equals(GetSideForUnit(unitId), "blue", StringComparison.Ordinal);

    public static bool IsRedForceUnit(string unitId) =>
        string.Equals(GetSideForUnit(unitId), "red", StringComparison.Ordinal);

    /// <summary>First registered (or legacy) blue unit id for red-on-blue engage fallback.</summary>
    public static string? GetDefaultBlueUnitId()
    {
        lock (Gate)
        {
            if (_scenarioSides != null)
            {
                foreach (var kv in _scenarioSides.OrderBy(k => k.Key, StringComparer.Ordinal))
                {
                    if (string.Equals(kv.Value, "blue", StringComparison.Ordinal))
                    {
                        return kv.Key;
                    }
                }
            }
        }

        return "u1";
    }

    /// <summary>First registered red unit id (no legacy default when catalog ORBAT present).</summary>
    public static string? GetDefaultRedUnitId()
    {
        lock (Gate)
        {
            if (_scenarioSides != null)
            {
                foreach (var kv in _scenarioSides.OrderBy(k => k.Key, StringComparer.Ordinal))
                {
                    if (string.Equals(kv.Value, "red", StringComparison.Ordinal))
                    {
                        return kv.Key;
                    }
                }
            }
        }

        return "hostile-1";
    }
}
