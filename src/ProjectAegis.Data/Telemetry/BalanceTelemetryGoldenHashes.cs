namespace ProjectAegis.Data.Telemetry;

/// <summary>Pinned accumulator state hashes for CI golden scenarios (S22-06).</summary>
public static class BalanceTelemetryGoldenHashes
{
    /// <summary>No-op sink / disabled feature flag.</summary>
    public const string EmptyState = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    /// <summary>Fixture sequence in <c>BalanceTelemetryGoldenTests</c>.</summary>
    public const string GoldenFixtureSequence = "61cc768e27ffcfc23b4188b0b808c2b329ac7f9d64fa8b8ec530ce011ca96bee";
}