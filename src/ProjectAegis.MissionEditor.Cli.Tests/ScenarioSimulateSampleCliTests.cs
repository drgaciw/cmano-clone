namespace ProjectAegis.MissionEditor.Cli.Tests;

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

public sealed class ScenarioSimulateSampleCliTests
{
    [Fact]
    public void scenario_simulate_sample_clean_fixture_returns_exit_0_with_world_hash()
    {
        var path = ScenarioValidationFixturePaths.Require("golden_clean.json");

        using var writer = new StringWriter();
        Assert.Equal(0, ScenarioSimulateSampleCommand.Run(path, ticks: 16, quiet: false, writer));
        var json = writer.ToString();
        Assert.Contains("worldHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fingerprint", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void scenario_simulate_sample_golden_clean_32_ticks_matches_pinned_world_hash()
    {
        var path = ScenarioValidationFixturePaths.Require("golden_clean.json");

        using var writer = new StringWriter();
        Assert.Equal(0, ScenarioSimulateSampleCommand.Run(path, SimulateSampleGoldenHashes.GoldenTicks, quiet: false, writer));

        var (json, _) = SplitSimulateSampleOutput(writer.ToString());
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(SimulateSampleGoldenHashes.GoldenCleanWorldHash, root.GetProperty("worldHash").GetString());
        Assert.Equal(SimulateSampleGoldenHashes.GoldenCleanEngagementCount, root.GetProperty("engagementCount").GetInt32());
        Assert.Equal(SimulateSampleGoldenHashes.GoldenCleanDetectionWorldHash, root.GetProperty("detectionWorldHash").GetString());
        Assert.Equal(SimulateSampleGoldenHashes.GoldenCleanWorldStateSha256, root.GetProperty("worldStateSha256").GetString());
        Assert.Equal("baltic-patrol-catalog", root.GetProperty("scenarioPolicyId").GetString());
    }

    [Fact]
    public void scenario_simulate_sample_unreachable_fixture_blocked_by_validation()
    {
        var path = ScenarioValidationFixturePaths.Require("golden_strike_unreachable.json");

        using var writer = new StringWriter();
        Assert.Equal(1, ScenarioSimulateSampleCommand.Run(path, ticks: 8, quiet: true, writer));
    }

    [Fact]
    public void scenario_simulate_sample_emits_fire_order_seed_hash_contract()
    {
        var path = ScenarioValidationFixturePaths.Require("golden_clean.json");

        using var writer = new StringWriter();
        Assert.Equal(0, ScenarioSimulateSampleCommand.Run(path, ticks: 16, quiet: false, writer));

        var (json, seedLine) = SplitSimulateSampleOutput(writer.ToString());
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("fire_order", out var fireOrder));
        Assert.Equal(JsonValueKind.Array, fireOrder.ValueKind);
        Assert.Equal(0, fireOrder.GetArrayLength());
        Assert.Equal("[]", CanonicalFireOrderJson(fireOrder));

        Assert.True(root.TryGetProperty("worldStateSha256", out var worldStateSha256));
        var hash = worldStateSha256.GetString();
        Assert.NotNull(hash);
        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-f]{64}$", hash);

        Assert.StartsWith("SEED=", seedLine, StringComparison.Ordinal);
        Assert.Contains($"HASH={hash}", seedLine, StringComparison.Ordinal);
        Assert.Contains("SEED=42", seedLine, StringComparison.Ordinal);
    }

    [Fact]
    public void scenario_simulate_sample_determinism_two_runs_identical_fire_order_and_hash()
    {
        var path = ScenarioValidationFixturePaths.Require("golden_clean.json");

        using var firstWriter = new StringWriter();
        using var secondWriter = new StringWriter();
        Assert.Equal(0, ScenarioSimulateSampleCommand.Run(path, SimulateSampleGoldenHashes.GoldenTicks, quiet: false, firstWriter));
        Assert.Equal(0, ScenarioSimulateSampleCommand.Run(path, SimulateSampleGoldenHashes.GoldenTicks, quiet: false, secondWriter));

        var (firstJson, firstSeedLine) = SplitSimulateSampleOutput(firstWriter.ToString());
        var (secondJson, secondSeedLine) = SplitSimulateSampleOutput(secondWriter.ToString());

        using var firstDoc = JsonDocument.Parse(firstJson);
        using var secondDoc = JsonDocument.Parse(secondJson);
        var firstRoot = firstDoc.RootElement;
        var secondRoot = secondDoc.RootElement;

        var firstFireOrderBytes = CanonicalFireOrderJson(firstRoot.GetProperty("fire_order"));
        var secondFireOrderBytes = CanonicalFireOrderJson(secondRoot.GetProperty("fire_order"));
        Assert.Equal(firstFireOrderBytes, secondFireOrderBytes);
        Assert.Equal("[]", firstFireOrderBytes);
        Assert.Equal(
            firstRoot.GetProperty("worldStateSha256").GetString(),
            secondRoot.GetProperty("worldStateSha256").GetString());
        Assert.Equal(firstSeedLine, secondSeedLine);
        Assert.Equal(firstJson, secondJson);
        Assert.Equal(SimulateSampleGoldenHashes.GoldenCleanWorldHash, firstRoot.GetProperty("worldHash").GetString());
        Assert.Equal(SimulateSampleGoldenHashes.GoldenCleanWorldHash, secondRoot.GetProperty("worldHash").GetString());
    }

    private static string CanonicalFireOrderJson(JsonElement fireOrder)
    {
        var ids = fireOrder.EnumerateArray().Select(element => element.GetString()).ToArray();
        return JsonSerializer.Serialize(ids);
    }

    private static (string Json, string SeedLine) SplitSimulateSampleOutput(string output)
    {
        var marker = output.LastIndexOf("SEED=", StringComparison.Ordinal);
        Assert.True(marker >= 0, "Expected SEED= stdout contract line after JSON output.");
        var json = output[..marker].Trim();
        var seedLine = output[marker..].Trim();
        return (json, seedLine);
    }

    // Helper for examples (used for non-empty fire_order AC-2 cases); walks for data/scenarios/examples
    // (examples not auto-copied like validation/ assets). See sprint-85 plan: use golden_clean + examples with events.
    private static string RequireExample(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 12; i++)
        {
            if (dir == null) break;
            var candidate = Path.Combine(dir.FullName, "data", "scenarios", "examples", fileName);
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        // Fallbacks for worktree / bin layouts
        dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 15; i++)
        {
            if (dir == null) break;
            var candidate = Path.Combine(dir.FullName, "..", "data", "scenarios", "examples", fileName);
            if (File.Exists(candidate)) return Path.GetFullPath(candidate);
            candidate = Path.Combine(dir.FullName, "..", "..", "..", "..", "data", "scenarios", "examples", fileName);
            if (File.Exists(candidate)) return Path.GetFullPath(candidate);
            dir = dir.Parent;
        }
        Assert.Fail($"Required example fixture '{fileName}' not found under data/scenarios/examples. Ensure repo layout.");
        return null!;
    }

    [Fact]
    public void scenario_simulate_sample_emits_nonempty_fire_order_for_mission_timeline()
    {
        // Non-empty case per AC-2 / qa-plan #2: mission policy with fireOrder (e.g. baltic-patrol-mission)
        var path = RequireExample("strike-package.scenario.json");

        using var writer = new StringWriter();
        Assert.Equal(0, ScenarioSimulateSampleCommand.Run(path, ticks: 4, quiet: false, writer));

        var (json, seedLine) = SplitSimulateSampleOutput(writer.ToString());
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("fire_order", out var fireOrder));
        Assert.Equal(JsonValueKind.Array, fireOrder.ValueKind);
        var ids = fireOrder.EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Equal(new[] { "start-exec", "contact-window" }, ids);
        Assert.Equal("[\"start-exec\",\"contact-window\"]", CanonicalFireOrderJson(fireOrder));

        Assert.True(root.TryGetProperty("worldStateSha256", out var worldStateSha256));
        var hash = worldStateSha256.GetString();
        Assert.NotNull(hash);
        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-f]{64}$", hash);

        Assert.StartsWith("SEED=", seedLine, StringComparison.Ordinal);
        Assert.Contains($"HASH={hash}", seedLine, StringComparison.Ordinal);
        Assert.Contains("SEED=1337", seedLine, StringComparison.Ordinal);
    }

    [Fact]
    public void scenario_simulate_sample_determinism_two_runs_identical_fire_order_and_hash_nonempty()
    {
        // AC-2: same seed (from metadata) + knobs => byte-identical fire_order + worldStateSha256 (excl editorState)
        var path = RequireExample("strike-package.scenario.json");

        using var firstWriter = new StringWriter();
        using var secondWriter = new StringWriter();
        Assert.Equal(0, ScenarioSimulateSampleCommand.Run(path, ticks: 4, quiet: false, firstWriter));
        Assert.Equal(0, ScenarioSimulateSampleCommand.Run(path, ticks: 4, quiet: false, secondWriter));

        var (firstJson, firstSeedLine) = SplitSimulateSampleOutput(firstWriter.ToString());
        var (secondJson, secondSeedLine) = SplitSimulateSampleOutput(secondWriter.ToString());

        using var firstDoc = JsonDocument.Parse(firstJson);
        using var secondDoc = JsonDocument.Parse(secondJson);
        var firstRoot = firstDoc.RootElement;
        var secondRoot = secondDoc.RootElement;

        var firstFireOrderBytes = CanonicalFireOrderJson(firstRoot.GetProperty("fire_order"));
        var secondFireOrderBytes = CanonicalFireOrderJson(secondRoot.GetProperty("fire_order"));
        Assert.Equal(firstFireOrderBytes, secondFireOrderBytes);
        Assert.Equal("[\"start-exec\",\"contact-window\"]", firstFireOrderBytes);
        Assert.Equal(
            firstRoot.GetProperty("worldStateSha256").GetString(),
            secondRoot.GetProperty("worldStateSha256").GetString());
        Assert.Equal(firstSeedLine, secondSeedLine);
        Assert.Equal(firstJson, secondJson);
    }

    [Fact]
    public void scenario_simulate_sample_determinism_holds_under_parallel_execution_isolation()
    {
        // Verifies sim isolation (no shared event queues/state) under parallel runs; required for CI parallel.
        // See sprint plan: "Test holds under parallel CI (isolation)".
        var path = ScenarioValidationFixturePaths.Require("golden_clean.json");

        var task1 = Task.Run(() =>
        {
            using var writer = new StringWriter();
            ScenarioSimulateSampleCommand.Run(path, ticks: 16, quiet: false, writer);
            return SplitSimulateSampleOutput(writer.ToString());
        });
        var task2 = Task.Run(() =>
        {
            using var writer = new StringWriter();
            ScenarioSimulateSampleCommand.Run(path, ticks: 16, quiet: false, writer);
            return SplitSimulateSampleOutput(writer.ToString());
        });

        var (json1, seed1) = task1.Result;
        var (json2, seed2) = task2.Result;

        Assert.Equal(json1, json2);
        Assert.Equal(seed1, seed2);
    }

    [Fact]
    public void scenario_simulate_sample_determinism_different_seed_control_yields_different_hash()
    {
        // Negative control: different metadata.seed produces different worldStateSha256 (fire_order shape may match if policy-driven).
        // Uses clean (seed 42) vs strike (seed 1337) for different inputs exercising contract.
        var pathClean = ScenarioValidationFixturePaths.Require("golden_clean.json");
        var pathStrike = RequireExample("strike-package.scenario.json");

        using var wClean = new StringWriter();
        using var wStrike = new StringWriter();
        Assert.Equal(0, ScenarioSimulateSampleCommand.Run(pathClean, ticks: 4, quiet: false, wClean));
        Assert.Equal(0, ScenarioSimulateSampleCommand.Run(pathStrike, ticks: 4, quiet: false, wStrike));

        var (jClean, _) = SplitSimulateSampleOutput(wClean.ToString());
        var (jStrike, _) = SplitSimulateSampleOutput(wStrike.ToString());
        using var dClean = JsonDocument.Parse(jClean);
        using var dStrike = JsonDocument.Parse(jStrike);

        var hashClean = dClean.RootElement.GetProperty("worldStateSha256").GetString();
        var hashStrike = dStrike.RootElement.GetProperty("worldStateSha256").GetString();
        Assert.NotEqual(hashClean, hashStrike);
        // fire_order shape differs as policies differ (empty vs nonempty) but contract holds independently.
    }
}