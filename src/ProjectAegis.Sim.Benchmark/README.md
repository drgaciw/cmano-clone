# ProjectAegis.Sim.Benchmark (INF-5.1)

Headless, managed sim-throughput benchmark. No Unity player loop — pure `dotnet`.

```bash
dotnet run -c Release --project src/ProjectAegis.Sim.Benchmark -- \
  [--ticks N] [--reps N] [--warmup N] [--seed N] [--out path] [--format csv|json] [--budget]
```

- Default: `--ticks 5000000 --reps 3 --warmup 200000 --seed 42 --format csv`.
- `--budget` prints the per-entity ns budget implied by the 256× and 1000× targets at 1k/5k/10k/25k entities.
- The CSV/JSON artifact records `entity_count, ticks, wall_clock_ms, ticks_per_second, sim_seconds,
  effective_realtime_multiple` (INF-5.1).

**Mode today: `core-tick`** — measures the sim's fixed per-tick cost (`SimTickPipeline.TickOnce`), so
`entity_count` is `0`. The managed sim has no per-entity per-tick workload yet, so the 25k@1000× NFR is
not directly measurable — the benchmark instead derives the per-entity compute budget that workload must
hit. See [`docs/reports/sim-entity-scale-benchmark-2026-07-08.md`](../../docs/reports/sim-entity-scale-benchmark-2026-07-08.md).

Deterministic parts (budget math, CSV/JSON formatting, result derivations) are covered by
`ProjectAegis.Sim.Tests` → `Benchmark/SimBenchmarkTests.cs`.
