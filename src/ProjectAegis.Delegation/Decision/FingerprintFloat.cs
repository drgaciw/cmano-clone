namespace ProjectAegis.Delegation.Decision;

using System.Globalization;

/// <summary>
/// Culture-invariant, ULP-stable formatting for floating-point values embedded in replay fingerprints.
/// Prevents non-deterministic fingerprints across cultures (decimal separator) and platforms
/// (<c>double</c> round-trip tail noise, e.g. <c>0.1 + 0.2</c>). See
/// docs/reports/baltic-headless-slice-gate-2026-07-04.md (concern #2 / recommendation #2).
/// Mirrors the invariant-culture precedent in
/// <c>ProjectAegis.Delegation.Hindsight.AgentDecisionMemoryFormatter</c>.
/// </summary>
internal static class FingerprintFloat
{
    /// <summary>
    /// Fractional values (scores, RNG draws, probabilities, fuel, HP): invariant culture, max 6 decimals,
    /// trailing zeros trimmed so already-clean values render unchanged (<c>1 -&gt; "1"</c>, <c>0.6 -&gt; "0.6"</c>)
    /// while platform-specific tails are quantized (<c>3.1331311245863662 -&gt; "3.133131"</c>).
    /// </summary>
    public static string Format(double value) => NormalizeNegativeZero(value).ToString("0.######", CultureInfo.InvariantCulture);

    /// <summary>
    /// Integer-valued sim-time ticks: round-trip form under invariant culture. Keeps the existing
    /// integer rendering (no golden churn) while removing the culture-sensitivity of a bare <c>ToString("R")</c>.
    /// </summary>
    public static string Time(double value) => NormalizeNegativeZero(value).ToString("R", CultureInfo.InvariantCulture);

    /// <summary>
    /// IEEE-754 negative zero (<c>-0.0</c>) is numerically equal to positive zero (<c>-0.0 == 0.0</c> is
    /// <c>true</c>), but <see cref="double.ToString(string, IFormatProvider)"/> renders it as <c>"-0"</c>
    /// instead of <c>"0"</c>. Any arithmetic path that can produce <c>-0.0</c> for a "no change" quantity
    /// (e.g. a subtraction with a different operand order) would otherwise desync the replay fingerprint from
    /// an otherwise state-identical run. Collapsing to <c>+0.0</c> here keeps the fingerprint a pure function
    /// of simulated value, not of the sign bit of an equivalent zero.
    /// </summary>
    private static double NormalizeNegativeZero(double value) => value == 0 ? 0.0 : value;
}
