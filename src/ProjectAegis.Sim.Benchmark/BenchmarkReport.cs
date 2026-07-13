namespace ProjectAegis.Sim.Benchmark;

using System.Globalization;
using System.Text;

/// <summary>CSV / JSON serialization of <see cref="BenchmarkResult"/> rows (INF-5.1 artifact).</summary>
public static class BenchmarkReport
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public const string CsvHeader =
        "mode,entity_count,ticks,repetitions,total_ticks,wall_clock_ms,ticks_per_second,sim_seconds,effective_realtime_multiple";

    public static string ToCsv(IReadOnlyList<BenchmarkResult> rows)
    {
        var sb = new StringBuilder();
        sb.Append(CsvHeader).Append('\n');
        foreach (var r in rows)
        {
            sb.Append(r.Mode).Append(',')
              .Append(r.EntityCount.ToString(Inv)).Append(',')
              .Append(r.Ticks.ToString(Inv)).Append(',')
              .Append(r.Repetitions.ToString(Inv)).Append(',')
              .Append(r.TotalTicks.ToString(Inv)).Append(',')
              .Append(F(r.WallClockMs)).Append(',')
              .Append(F(r.TicksPerSecond)).Append(',')
              .Append(F(r.SimSeconds)).Append(',')
              .Append(F(r.EffectiveRealtimeMultiple)).Append('\n');
        }

        return sb.ToString();
    }

    public static string ToJson(IReadOnlyList<BenchmarkResult> rows)
    {
        var sb = new StringBuilder();
        sb.Append("[\n");
        for (var i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            sb.Append("  {")
              .Append("\"mode\":\"").Append(r.Mode).Append("\",")
              .Append("\"entity_count\":").Append(r.EntityCount.ToString(Inv)).Append(',')
              .Append("\"ticks\":").Append(r.Ticks.ToString(Inv)).Append(',')
              .Append("\"repetitions\":").Append(r.Repetitions.ToString(Inv)).Append(',')
              .Append("\"total_ticks\":").Append(r.TotalTicks.ToString(Inv)).Append(',')
              .Append("\"wall_clock_ms\":").Append(F(r.WallClockMs)).Append(',')
              .Append("\"ticks_per_second\":").Append(F(r.TicksPerSecond)).Append(',')
              .Append("\"sim_seconds\":").Append(F(r.SimSeconds)).Append(',')
              .Append("\"effective_realtime_multiple\":").Append(F(r.EffectiveRealtimeMultiple))
              .Append('}');
            sb.Append(i < rows.Count - 1 ? ",\n" : "\n");
        }

        sb.Append("]\n");
        return sb.ToString();
    }

    private static string F(double value) => value.ToString("0.###", Inv);
}
