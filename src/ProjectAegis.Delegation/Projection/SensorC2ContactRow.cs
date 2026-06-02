namespace ProjectAegis.Delegation.Projection;

/// <summary>One contact row for sensor C2 list binding (UI Toolkit / headless tests).</summary>
public sealed record SensorC2ContactRow(
    string ContactId,
    string LifecycleState,
    string TargetId,
    string DisplayLine);