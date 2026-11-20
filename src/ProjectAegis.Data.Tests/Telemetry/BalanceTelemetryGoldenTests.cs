using ProjectAegis.Data.Telemetry;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Telemetry;

public sealed class BalanceTelemetryGoldenTests
{
    [Test]
    public void Golden_fixture_sequence_produces_stable_state_hash()
    {
        var sink = new BalanceTelemetryAccumulator(new BalanceDriftOptions
        {
            MinimumSampleRuns = 50,
            WinRateDriftThreshold = 0.08,
            DefaultExpectedWinRate = 0.5,
        });

        sink.RegisterExpectedWinRate("fixture-platform", 0.5);
        sink.RegisterExpectedWinRate("fixture-weapon", 0.5);

        var outcomes = new (string EntityId, BalanceEntityKind Kind, bool Won)[]
        {
            ("fixture-platform", BalanceEntityKind.Platform, true),
            ("fixture-platform", BalanceEntityKind.Platform, false),
            ("fixture-weapon", BalanceEntityKind.Weapon, true),
            ("fixture-platform", BalanceEntityKind.Platform, true),
            ("fixture-weapon", BalanceEntityKind.Weapon, false),
        };

        foreach (var (entityId, kind, won) in outcomes)
        {
            sink.RecordOutcome(entityId, kind, won);
        }

        for (var i = 0; i < 48; i++)
        {
            sink.RecordOutcome("fixture-platform", BalanceEntityKind.Platform, won: i % 3 != 0);
        }

        for (var i = 0; i < 48; i++)
        {
            sink.RecordOutcome("fixture-weapon", BalanceEntityKind.Weapon, won: i < 40);
        }

        var hash = sink.ComputeStateHash();
        Assert.That(sink.ComputeStateHash(), Is.EqualTo(hash));
        Assert.That(hash, Is.EqualTo(BalanceTelemetryGoldenHashes.GoldenFixtureSequence));

        var report = sink.EvaluateDrift();
        Assert.That(report.StateHash, Is.EqualTo(hash));
        Assert.That(report.Findings.Any(f => f.EntityId == "fixture-weapon"), Is.True);
    }
}