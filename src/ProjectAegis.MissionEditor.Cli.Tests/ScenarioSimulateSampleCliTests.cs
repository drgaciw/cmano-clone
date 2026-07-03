namespace ProjectAegis.MissionEditor.Cli.Tests;

using System.Text.Json;
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
}