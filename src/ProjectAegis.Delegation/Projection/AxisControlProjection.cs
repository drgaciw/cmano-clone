namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// CMD-19: per-axis Manual vs Automatic control readable without opening a menu.
/// UI-local state bag; issuance still goes through existing command path.
/// </summary>
public static class AxisControlProjection
{
    public static AxisControlState ProjectDefault(string unitId) =>
        new(
            UnitId: unitId ?? string.Empty,
            AltitudeAxis: AxisMode.Automatic,
            ThrottleAxis: AxisMode.Automatic,
            EmconAxis: AxisMode.Automatic,
            SensorsAxis: AxisMode.Automatic);

    public static AxisControlState Toggle(AxisControlState state, AxisKind axis)
    {
        if (state is null)
        {
            return ProjectDefault(string.Empty);
        }

        return axis switch
        {
            AxisKind.Altitude => state with { AltitudeAxis = Flip(state.AltitudeAxis) },
            AxisKind.Throttle => state with { ThrottleAxis = Flip(state.ThrottleAxis) },
            AxisKind.Emcon => state with { EmconAxis = Flip(state.EmconAxis) },
            AxisKind.Sensors => state with { SensorsAxis = Flip(state.SensorsAxis) },
            _ => state,
        };
    }

    public static string FormatBadge(AxisControlState state)
    {
        if (state is null)
        {
            return "AXES: —";
        }

        var parts = new[]
        {
            $"ALT:{ModeToken(state.AltitudeAxis)}",
            $"THR:{ModeToken(state.ThrottleAxis)}",
            $"EMC:{ModeToken(state.EmconAxis)}",
            $"SNS:{ModeToken(state.SensorsAxis)}",
        };
        return "AXES " + string.Join(" ", parts);
    }

    public static bool IsMixed(AxisControlState state)
    {
        if (state is null)
        {
            return false;
        }

        var modes = new[] { state.AltitudeAxis, state.ThrottleAxis, state.EmconAxis, state.SensorsAxis };
        return modes.Any(m => m == AxisMode.Manual) && modes.Any(m => m == AxisMode.Automatic);
    }

    private static AxisMode Flip(AxisMode mode) =>
        mode == AxisMode.Manual ? AxisMode.Automatic : AxisMode.Manual;

    private static string ModeToken(AxisMode mode) => mode == AxisMode.Manual ? "MAN" : "AUTO";
}

public enum AxisKind
{
    Altitude,
    Throttle,
    Emcon,
    Sensors,
}

public enum AxisMode
{
    Manual,
    Automatic,
}

public sealed record AxisControlState(
    string UnitId,
    AxisMode AltitudeAxis,
    AxisMode ThrottleAxis,
    AxisMode EmconAxis,
    AxisMode SensorsAxis);
