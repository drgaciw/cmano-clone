namespace ProjectAegis.Delegation.Tests.Decision;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using NUnit.Framework;

/// <summary>
/// Regression for a determinism hazard in <c>FingerprintFloat</c> (the invariant-culture replay-fingerprint
/// formatter). IEEE-754 negative zero (<c>-0.0</c>) is numerically equal to positive zero
/// (<c>-0.0 == 0.0</c> is <c>true</c>), but <c>"0.######"</c> / <c>"R"</c> formatting renders them as different
/// text (<c>"-0"</c> vs <c>"0"</c>). Any arithmetic path that can yield <c>-0.0</c> for a "no change" quantity
/// (e.g. a fuel-burn delta of zero reached via subtraction in a different operand order, or a future refactor
/// of a damage/score calculation) would silently desync the replay fingerprint from an otherwise
/// state-identical run — a "hash changes without content change" determinism break.
/// </summary>
[TestFixture]
public sealed class FingerprintFloatNegativeZeroTests
{
    [Test]
    public void FuelBurn_fingerprint_treats_negative_zero_delta_same_as_positive_zero()
    {
        var logWithPositiveZero = new DecisionLog();
        logWithPositiveZero.AppendFuelBurn(new FuelBurnRecord(0, 10.0, 10, new TargetId("u1"), 0.0, 500.0));

        var logWithNegativeZero = new DecisionLog();
        logWithNegativeZero.AppendFuelBurn(new FuelBurnRecord(0, 10.0, 10, new TargetId("u1"), -0.0, 500.0));

        var fingerprintWithPositiveZero = logWithPositiveZero.ComputeFingerprint();
        var fingerprintWithNegativeZero = logWithNegativeZero.ComputeFingerprint();

        Assert.That(
            fingerprintWithNegativeZero,
            Is.EqualTo(fingerprintWithPositiveZero),
            "DeltaKg=-0.0 and DeltaKg=0.0 both mean 'no fuel burned this tick' (-0.0 == 0.0) but the replay " +
            "fingerprint renders them as different text (\"-0\" vs \"0\"), which would desync the replay hash " +
            "for a logically identical simulation state depending on which arithmetic path produced the zero.");
    }
}
