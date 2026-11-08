namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

/// <summary>
/// CMD-36: headless C2 panel-bind wall-clock sample stats (mean / p95 / max).
/// Tests-only helper — not a production hotpath type.
/// </summary>
public sealed class C2PanelPerfSample
{
    public C2PanelPerfSample(IReadOnlyList<double> samplesMs)
    {
        if (samplesMs is null)
        {
            throw new ArgumentNullException(nameof(samplesMs));
        }

        if (samplesMs.Count == 0)
        {
            throw new ArgumentException("At least one sample is required.", nameof(samplesMs));
        }

        var sorted = samplesMs.ToArray();
        Array.Sort(sorted);

        N = sorted.Length;
        MeanMs = sorted.Average();
        MaxMs = sorted[^1];
        var p95Index = (int)Math.Ceiling(N * 0.95) - 1;
        if (p95Index < 0)
        {
            p95Index = 0;
        }

        if (p95Index >= N)
        {
            p95Index = N - 1;
        }

        P95Ms = sorted[p95Index];
        SamplesMs = sorted;
    }

    public int N { get; }
    public double MeanMs { get; }
    public double P95Ms { get; }
    public double MaxMs { get; }
    public IReadOnlyList<double> SamplesMs { get; }

    public string FormatReport(string label = "C2 panel bind") =>
        $"{label}: mean={MeanMs:F3} ms p95={P95Ms:F3} ms max={MaxMs:F3} ms (n={N})";
}
