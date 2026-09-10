namespace ProjectAegis.Delegation.Decision;

using Core;

/// <summary>Per-tick fuel burn delta when <see cref="ProjectAegis.Sim.Scenario.ScenarioLogisticsSettings.LogTickBurn"/> is enabled.</summary>
public sealed record FuelBurnRecord(
    ulong SequenceId,
    double SimTime,
    ulong SimTick,
    TargetId UnitId,
    double DeltaKg,
    double RemainingFuelKg);