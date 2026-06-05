namespace ProjectAegis.Delegation.Decision;

using ProjectAegis.Delegation.Core;

/// <summary>Per-tick fuel burn delta when <see cref="ScenarioLogisticsSettings.LogTickBurn"/> is enabled.</summary>
public sealed record FuelBurnRecord(
    ulong SequenceId,
    double SimTime,
    ulong SimTick,
    TargetId UnitId,
    double DeltaKg,
    double RemainingFuelKg);