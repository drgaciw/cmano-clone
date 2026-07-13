namespace ProjectAegis.Data.Telemetry;

using System.Globalization;

/// <summary>Real win-rate accumulator with advisory ±8% drift flags (DBI-5.3).</summary>
public sealed class BalanceTelemetryAccumulator : IBalanceTelemetrySink
{
    private readonly BalanceDriftOptions _options;
    private readonly SortedDictionary<string, EntityAccumulator> _entities;
    private readonly SortedDictionary<string, double> _expectedWinRateOverrides;

    public BalanceTelemetryAccumulator(BalanceDriftOptions? options = null)
    {
        _options = options ?? new BalanceDriftOptions();
        _entities = new SortedDictionary<string, EntityAccumulator>(StringComparer.Ordinal);
        _expectedWinRateOverrides = new SortedDictionary<string, double>(StringComparer.Ordinal);
    }

    public void RecordOutcome(string entityId, BalanceEntityKind entityKind, bool won)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity id is required.", nameof(entityId));
        }

        var key = MakeKey(entityId, entityKind);
        if (!_entities.TryGetValue(key, out var stats))
        {
            var expected = ResolveExpectedWinRate(entityId);
            stats = new EntityAccumulator(entityId, entityKind, expected);
            _entities[key] = stats;
        }

        stats.Record(won);
    }

    public void RegisterExpectedWinRate(string entityId, double expectedWinRate)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity id is required.", nameof(entityId));
        }

        if (expectedWinRate is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedWinRate), "Expected win rate must be in [0, 1].");
        }

        _expectedWinRateOverrides[entityId] = expectedWinRate;
        foreach (var pair in _entities.Values.Where(e => e.EntityId == entityId))
        {
            pair.SetExpectedWinRate(expectedWinRate);
        }
    }

    private double ResolveExpectedWinRate(string entityId) =>
        _expectedWinRateOverrides.TryGetValue(entityId, out var expected)
            ? expected
            : _options.DefaultExpectedWinRate;

    public BalanceDriftReport EvaluateDrift()
    {
        var findings = BuildFindings();
        return new BalanceDriftReport(
            DriftDetectionEnabled: true,
            Findings: findings,
            StateHash: ComputeStateHash(findings));
    }

    public string ComputeStateHash() => ComputeStateHash(BuildFindings());

    private IReadOnlyList<BalanceDriftFinding> BuildFindings()
    {
        var findings = new List<BalanceDriftFinding>();
        foreach (var stats in _entities.Values
                     .OrderBy(e => e.EntityId, StringComparer.Ordinal)
                     .ThenBy(e => (int)e.EntityKind))
        {
            if (stats.TotalRuns < _options.MinimumSampleRuns)
            {
                continue;
            }

            var actual = stats.ActualWinRate;
            var drift = actual - stats.ExpectedWinRate;
            if (Math.Abs(drift) <= _options.WinRateDriftThreshold)
            {
                continue;
            }

            findings.Add(new BalanceDriftFinding(
                Code: "BALANCE_WIN_RATE_DRIFT",
                EntityId: stats.EntityId,
                EntityKind: stats.EntityKind,
                SampleRuns: stats.TotalRuns,
                ExpectedWinRate: stats.ExpectedWinRate,
                ActualWinRate: actual,
                DriftDelta: drift,
                Message: string.Format(
                    CultureInfo.InvariantCulture,
                    "Win-rate drift {0:P1} exceeds ±{1:P0} over {2} runs.",
                    drift,
                    _options.WinRateDriftThreshold,
                    stats.TotalRuns)));
        }

        return findings;
    }

    private string ComputeStateHash(IReadOnlyList<BalanceDriftFinding> findings)
    {
        var snapshots = _entities.Values
            .Select(e => new BalanceTelemetryEntitySnapshot(
                e.EntityId,
                e.EntityKind,
                e.Wins,
                e.TotalRuns,
                e.ExpectedWinRate))
            .ToArray();
        return BalanceTelemetryStateHasher.Compute(snapshots, findings);
    }

    private static string MakeKey(string entityId, BalanceEntityKind entityKind) =>
        $"{entityId}\u001f{(int)entityKind}";

    private sealed class EntityAccumulator
    {
        public EntityAccumulator(string entityId, BalanceEntityKind entityKind, double expectedWinRate)
        {
            EntityId = entityId;
            EntityKind = entityKind;
            ExpectedWinRate = expectedWinRate;
        }

        public string EntityId { get; }

        public BalanceEntityKind EntityKind { get; }

        public double ExpectedWinRate { get; private set; }

        public int Wins { get; private set; }

        public int TotalRuns { get; private set; }

        public double ActualWinRate => TotalRuns == 0 ? 0 : (double)Wins / TotalRuns;

        public void Record(bool won)
        {
            TotalRuns++;
            if (won)
            {
                Wins++;
            }
        }

        public void SetExpectedWinRate(double expectedWinRate) => ExpectedWinRate = expectedWinRate;
    }
}