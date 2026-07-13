using System.Globalization;
using System.Text.Json;

namespace ProjectAegis.Data.Catalog;

/// <summary>
/// Fail-closed post-batch oracle evaluation for qa-gauntlet.
/// Parses <c>gauntlet.expect</c> from policy JSON and bounds from results CSV rows.
/// </summary>
public static class GauntletOracleEvaluator
{
    public static GauntletOracleEvaluationResult EvaluateFromPolicyAndCsv(
        string policyJson,
        string resultsCsv)
    {
        if (string.IsNullOrWhiteSpace(policyJson))
        {
            return new GauntletOracleEvaluationResult(false, ["missing policy json"]);
        }

        if (!TryParseExpect(policyJson, out var expect, out var parseFailures))
        {
            return new GauntletOracleEvaluationResult(false, parseFailures);
        }

        var rows = ParseCsvRows(resultsCsv);
        if (rows.Count == 0)
        {
            return new GauntletOracleEvaluationResult(false, ["no results rows"]);
        }

        return Evaluate(rows, expect!);
    }

    public static GauntletOracleEvaluationResult Evaluate(
        IReadOnlyList<GauntletBatchResultRow> rows,
        GauntletOracleExpect expect)
    {
        var failures = new List<string>();
        if (rows.Count == 0)
        {
            failures.Add("no results rows");
            return new GauntletOracleEvaluationResult(false, failures);
        }

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var prefix = $"row[{i}] scenario={row.ScenarioId} seed={row.Seed}";

            if (expect.RequireNonEmptyFingerprint
                && string.IsNullOrWhiteSpace(row.Fingerprint))
            {
                failures.Add($"{prefix}: empty fingerprint");
            }

            if (expect.Side != null
                && !string.Equals(expect.Side, row.Side, StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{prefix}: side expected '{expect.Side}' got '{row.Side}'");
            }

            if (expect.MinKills is int minKills && row.Kills < minKills)
            {
                failures.Add($"{prefix}: kills {row.Kills} < min {minKills}");
            }

            if (expect.MaxMissilesFired is int maxMissiles && row.MissilesFired > maxMissiles)
            {
                failures.Add($"{prefix}: missilesFired {row.MissilesFired} > max {maxMissiles}");
            }

            if (expect.MinDenials is int minDenials && row.Denials < minDenials)
            {
                failures.Add($"{prefix}: denials {row.Denials} < min {minDenials}");
            }

            if (expect.MaxDenials is int maxDenials && row.Denials > maxDenials)
            {
                failures.Add($"{prefix}: denials {row.Denials} > max {maxDenials}");
            }

            if (expect.MinScore is double minScore && row.Score < minScore)
            {
                failures.Add($"{prefix}: score {row.Score} < min {minScore}");
            }

            if (expect.MaxScore is double maxScore && row.Score > maxScore)
            {
                failures.Add($"{prefix}: score {row.Score} > max {maxScore}");
            }
        }

        return new GauntletOracleEvaluationResult(failures.Count == 0, failures);
    }

    public static bool TryParseExpect(
        string policyJson,
        out GauntletOracleExpect? expect,
        out IReadOnlyList<string> failures)
    {
        expect = null;
        var fails = new List<string>();
        try
        {
            using var doc = JsonDocument.Parse(policyJson);
            if (!doc.RootElement.TryGetProperty("gauntlet", out var gauntlet)
                || gauntlet.ValueKind != JsonValueKind.Object)
            {
                fails.Add("missing gauntlet.expect");
                failures = fails;
                return false;
            }

            if (!gauntlet.TryGetProperty("expect", out var exp)
                || exp.ValueKind != JsonValueKind.Object)
            {
                fails.Add("missing gauntlet.expect");
                failures = fails;
                return false;
            }

            string? side = exp.TryGetProperty("side", out var sideEl)
                && sideEl.ValueKind == JsonValueKind.String
                ? sideEl.GetString()
                : null;

            expect = new GauntletOracleExpect(
                Side: side,
                MinKills: ReadInt(exp, "minKills"),
                MaxMissilesFired: ReadInt(exp, "maxMissilesFired"),
                MinDenials: ReadInt(exp, "minDenials"),
                MaxDenials: ReadInt(exp, "maxDenials"),
                MinScore: ReadDouble(exp, "minScore"),
                MaxScore: ReadDouble(exp, "maxScore"),
                RequireNonEmptyFingerprint: !exp.TryGetProperty("requireNonEmptyFingerprint", out var fp)
                    || fp.ValueKind != JsonValueKind.False);

            failures = fails;
            return true;
        }
        catch (JsonException ex)
        {
            fails.Add($"invalid policy json: {ex.Message}");
            failures = fails;
            return false;
        }
    }

    public static IReadOnlyList<GauntletBatchResultRow> ParseCsvRows(string resultsCsv)
    {
        var rows = new List<GauntletBatchResultRow>();
        if (string.IsNullOrWhiteSpace(resultsCsv))
        {
            return rows;
        }

        var lines = resultsCsv.Split('\n');
        if (lines.Length < 2)
        {
            return rows;
        }

        var header = lines[0].Trim().Split(',');
        var idx = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < header.Length; i++)
        {
            idx[header[i].Trim()] = i;
        }

        int Col(string name) => idx.TryGetValue(name, out var i) ? i : -1;
        var cScenario = Col("scenarioId");
        var cSeed = Col("seed");
        var cSide = Col("side");
        var cScore = Col("score");
        var cKills = Col("kills");
        var cMissiles = Col("missilesFired");
        var cDenials = Col("denials");
        var cFp = Col("fingerprint");

        for (var li = 1; li < lines.Length; li++)
        {
            var line = lines[li].TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var cells = SplitCsvLine(line);
            string Get(int c) => c >= 0 && c < cells.Count ? cells[c] : "";

            rows.Add(new GauntletBatchResultRow(
                ScenarioId: Get(cScenario),
                Seed: Get(cSeed),
                Side: Get(cSide),
                Score: ParseDouble(Get(cScore)),
                Kills: ParseInt(Get(cKills)),
                MissilesFired: ParseInt(Get(cMissiles)),
                Denials: ParseInt(Get(cDenials)),
                Fingerprint: Get(cFp)));
        }

        return rows;
    }

    private static int? ReadInt(JsonElement exp, string name)
    {
        if (!exp.TryGetProperty(name, out var el))
        {
            return null;
        }

        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var i))
        {
            return i;
        }

        if (el.ValueKind == JsonValueKind.String
            && int.TryParse(el.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var j))
        {
            return j;
        }

        return null;
    }

    private static double? ReadDouble(JsonElement exp, string name)
    {
        if (!exp.TryGetProperty(name, out var el))
        {
            return null;
        }

        if (el.ValueKind == JsonValueKind.Number && el.TryGetDouble(out var d))
        {
            return d;
        }

        if (el.ValueKind == JsonValueKind.String
            && double.TryParse(el.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var e))
        {
            return e;
        }

        return null;
    }

    private static int ParseInt(string s) =>
        int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;

    private static double ParseDouble(string s) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;

    /// <summary>Minimal CSV split (handles no embedded commas in fingerprint by joining tail).</summary>
    private static List<string> SplitCsvLine(string line)
    {
        // Fingerprint is last column and may contain no commas in practice; if more cells than 8, join rest into fingerprint.
        var parts = line.Split(',');
        if (parts.Length <= 8)
        {
            return parts.ToList();
        }

        var cells = parts.Take(7).ToList();
        cells.Add(string.Join(",", parts.Skip(7)));
        return cells;
    }
}

/// <summary>One batch harness CSV result row.</summary>
public sealed record GauntletBatchResultRow(
    string ScenarioId,
    string Seed,
    string Side,
    double Score,
    int Kills,
    int MissilesFired,
    int Denials,
    string Fingerprint);
